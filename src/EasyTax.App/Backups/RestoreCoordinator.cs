using System.Diagnostics;
using System.Globalization;
using EasyTax.App.Infrastructure;
using EasyTax.App.Localization;
using EasyTax.App.Security;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Core.Backups;
using EasyTax.Core.Domain.Clients;
using EasyTax.Data;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace EasyTax.App.Backups;

/// <summary>
/// Restores a backup file: checks it, makes sure its key is known, removes the clients that were deleted after the backup was made,
/// saves a safety copy of the current data, swaps the database file and restarts the app. Nothing is replaced until every check passed.
/// </summary>
public sealed class RestoreCoordinator(
	IBackupService backups,
	IDeletionLog deletionLog,
	IClientRepository clients,
	BackupManager manager,
	DatabaseKeyService keys,
	EasyTaxDataOptions options,
	IDialogService dialogs,
	IPickerService pickers,
	IToastService toasts,
	ISettingsService settings,
	ILocalizer localizer,
	ILogger<RestoreCoordinator> logger)
{
	/// <summary>What the deletion step did to the restored copy.</summary>
	/// <param name="All">Every deletion known from the three sources (the backup's own record, the live database and the log file).</param>
	/// <param name="Removed">How many of those clients were still in the backup and were removed again.</param>
	/// <param name="SkippedLogLines">Damaged lines in the log file that could not be read.</param>
	private sealed record DeletionOutcome(IReadOnlyList<DeletedClient> All, int Removed, int SkippedLogLines);

	public async Task RestoreAsync()
	{
		var zipPath = await pickers.PickFileAsync(".zip");
		if (zipPath is null)
			return;

		BackupManifest manifest;
		try
		{
			manifest = await backups.ReadManifestAsync(zipPath);
		}
		catch (InvalidDataException)
		{
			toasts.Error(localizer["Backup_NotABackup"]);
			return;
		}

		if (backups.IsFromNewerVersion(manifest))
		{
			toasts.Error(localizer["Backup_TooNew"]);
			return;
		}

		var created = manifest.CreatedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
		var message = string.Format(CultureInfo.CurrentCulture, localizer["Restore_Confirm_Message"], created);
		if (!await dialogs.ConfirmAsync(localizer["Restore_Confirm_Title"], message, localizer["Restore_Confirm_Yes"], localizer["Dialog_Cancel"]))
			return;

		string? extracted = null;
		try
		{
			extracted = await backups.ExtractAsync(zipPath);

			var key = await FindKeyAsync(extracted);
			if (key is null)
				return;

			// The restored copy must have the current schema before anything is read from it or removed from it.
			await backups.MigrateAsync(extracted, key);

			var outcome = await RemoveDeletedClientsAsync(extracted, key);
			if (outcome is null)
				return;

			// If the safety copy cannot be made (unreachable folder), the restore stops here and nothing has changed.
			await manager.RunSafetyBackupAsync();

			backups.ReplaceLiveDatabase(extracted);
			keys.Store(key);

			await KeepLogCompleteAsync(outcome.All);
			await ShowSummaryAsync(created, outcome);

			Restart();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "The restore failed");

			var reason = UserFacingErrors.Describe(ex, localizer);
			toasts.Error(string.Format(CultureInfo.CurrentCulture, localizer["Toast_RestoreFailed"], reason));
		}
		finally
		{
			if (extracted is not null)
				backups.DiscardExtracted(extracted);
		}
	}

	// The key of this install opens backups made by this install. A backup from another computer needs that computer's recovery key.
	private async Task<string?> FindKeyAsync(string extractedDatabase)
	{
		var local = options.Password ?? throw new InvalidOperationException("The database key has not been set.");
		if (await backups.IsHealthyAsync(extractedDatabase, local))
			return local;

		var window = new KeyRecoveryWindow(localizer, settings.Current.Theme, key => backups.IsHealthyAsync(extractedDatabase, key), "Restore_Key_Message", "Restore_Key_Cancel");
		if (!await window.ShowAsync())
			return null;

		return window.Key;
	}

	/// <summary>
	/// Collects every deletion we know of (recorded in the backup itself, in the live database, which has the ones made since, and
	/// in the log file, which also survives the loss of the database) and removes those clients from the restored copy.
	/// Returns null when the user chose to stop.
	/// </summary>
	private async Task<DeletionOutcome?> RemoveDeletedClientsAsync(string extractedDatabase, string key)
	{
		var fromBackup = await backups.ReadDeletionsAsync(extractedDatabase, key);
		var fromLiveDatabase = await clients.GetDeletionsAsync();

		DeletionLogContent fromLog;
		try
		{
			fromLog = await deletionLog.ReadAsync(manager.Folder);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// A log we cannot read might hold deletions found nowhere else, so do not go on silently.
			logger.LogWarning(ex, "The deletion log could not be read for a restore");

			if (!await dialogs.ConfirmAsync(localizer["Restore_Confirm_Title"], localizer["Restore_LogUnreadable_Message"], localizer["Restore_LogUnreadable_Yes"], localizer["Dialog_Cancel"]))
				return null;

			fromLog = new DeletionLogContent([], 0);
		}

		var all = DeletedClient.Merge(fromBackup, fromLiveDatabase, fromLog.Entries);
		var removed = await backups.ApplyDeletionsAsync(extractedDatabase, key, all);

		return new DeletionOutcome(all, removed, fromLog.SkippedLines);
	}

	// The deletions now in the restored database are copied to the log too, so the file never knows less than the database.
	private async Task KeepLogCompleteAsync(IReadOnlyList<DeletedClient> all)
	{
		try
		{
			await deletionLog.AppendAsync(manager.Folder, all);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Not fatal: the next start copies everything from the database into the file again.
			logger.LogWarning(ex, "Could not merge the restored deletions into the deletion log");
		}
	}

	private async Task ShowSummaryAsync(string created, DeletionOutcome outcome)
	{
		var lines = new List<string>
		{
			string.Format(CultureInfo.CurrentCulture, localizer["Restore_Done_Message"], created),
			string.Format(CultureInfo.CurrentCulture, localizer["Restore_Done_Removed"], outcome.Removed),
		};

		if (outcome.SkippedLogLines > 0)
			lines.Add(string.Format(CultureInfo.CurrentCulture, localizer["Restore_Done_Skipped"], outcome.SkippedLogLines));

		await dialogs.ShowTextAsync(localizer["Restore_Done_Title"], string.Join(Environment.NewLine, lines), localizer["Restore_Done_Close"]);
	}

	// Installed (MSIX): Windows restarts the app itself, so the new process keeps its package identity. Launching the exe from
	// inside the package folder would not. AppInstance.Restart only returns when it failed.
	// Unpackaged (development): start a new process that waits for this one to end (see App.WaitForPreviousInstance), so the
	// single-instance check does not mistake it for a second copy.
	private void Restart()
	{
		if (PackageInfo.IsPackaged)
		{
			var reason = AppInstance.Restart(string.Empty);
			logger.LogError("Windows could not restart the app after a restore: {Reason}", reason);
			toasts.Warning(localizer["Toast_RestoreRestart"]);
			return;
		}

		try
		{
			Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"--restart-after {Environment.ProcessId}") { UseShellExecute = false });
		}
		catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
		{
			logger.LogError(ex, "Could not restart the app after a restore");
			toasts.Warning(localizer["Toast_RestoreRestart"]);
			return;
		}

		Application.Current.Exit();
	}
}
