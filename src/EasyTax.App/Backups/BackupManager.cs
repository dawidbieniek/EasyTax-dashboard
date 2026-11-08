using System.Globalization;
using EasyTax.App.Infrastructure;
using EasyTax.App.Localization;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Core.Backups;
using Microsoft.Extensions.Logging;

namespace EasyTax.App.Backups;

/// <summary>
/// Runs backups for the app: the button in Settings, the safety copy before a restore and the automatic one at startup.
/// Only one backup runs at a time. Results are reported with toasts.
/// </summary>
public sealed class BackupManager(
	IBackupService backups,
	ISettingsService settings,
	IToastService toasts,
	ClientDeletionService deletions,
	ILocalizer localizer,
	ILogger<BackupManager> logger)
{
	private static readonly TimeSpan Interval = TimeSpan.FromDays(7);

	private readonly SemaphoreSlim _running = new(1, 1);

	/// <summary>Raised after a backup finished (successfully or not), so the Settings page can refresh its status.</summary>
	public event EventHandler? Finished;

	/// <summary>The folder backups go to: the one set in Settings, else Documents\EasyTax\Backups.</summary>
	public string Folder => settings.Current.EffectiveBackupFolder;

	/// <summary>Brings the deletion log in the backup folder up to date (also called when the folder changed).</summary>
	public Task SyncDeletionLogAsync() => deletions.SyncLogAsync();

	public BackupInfo? Latest() => backups.GetLatestBackup(Folder);

	/// <summary>"Back up now". Returns true when a backup was made.</summary>
	public async Task<bool> RunNowAsync()
	{
		if (!await _running.WaitAsync(0))
			return false;

		try
		{
			await backups.RunAsync(Folder);
			await deletions.SyncLogAsync();
			toasts.Success(localizer["Toast_BackupDone"]);

			return true;
		}
		catch (Exception ex)
		{
			ReportFailure(ex);
			return false;
		}
		finally
		{
			_running.Release();
			Finished?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>Copy of the current data made right before a restore replaces it. Throws when it cannot be made, so the restore is stopped.</summary>
	public async Task RunSafetyBackupAsync()
	{
		await _running.WaitAsync();
		try
		{
			await backups.RunAsync(Folder, "EasyTax-prerestore");
		}
		finally
		{
			_running.Release();
		}
	}

	/// <summary>
	/// At startup: cleans up half-written backups and, when automatic backups are on and the newest backup is older than seven days
	/// (or there is none), makes one in the background. There is no timer: an app that never restarts is not backed up.
	/// </summary>
	public async Task RunScheduledIfDueAsync()
	{
		try
		{
			backups.DeleteStaleTempFiles(Folder);
			await deletions.SyncLogAsync();

			if (!settings.Current.ScheduledBackups)
				return;

			var latest = backups.GetLatestBackup(Folder);
			if (latest is not null && DateTimeOffset.Now - latest.Manifest.CreatedAt < Interval)
				return;

			await RunNowAsync();
		}
		catch (Exception ex)
		{
			// Never let a failed check take the app down; the cause is in the log.
			logger.LogError(ex, "The scheduled backup check failed");
		}
	}

	private void ReportFailure(Exception exception)
	{
		logger.LogError(exception, "The backup failed");

		var reason = UserFacingErrors.Describe(exception, localizer);
		toasts.Error(string.Format(CultureInfo.CurrentCulture, localizer["Toast_BackupFailed"], reason));
	}
}
