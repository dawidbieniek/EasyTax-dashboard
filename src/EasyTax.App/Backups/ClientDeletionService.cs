using EasyTax.App.Localization;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Core.Backups;
using EasyTax.Core.Domain.Clients;
using Microsoft.Extensions.Logging;

namespace EasyTax.App.Backups;

/// <summary>
/// The one place that deletes a client. The database records the deletion in the same transaction as the delete (see
/// <see cref="IClientRepository.DeleteAsync"/>); this service also writes it to <c>deletions.log</c> in the backup folder and
/// keeps that file complete. Together they make sure a restored backup cannot bring a deleted client back.
/// </summary>
public sealed class ClientDeletionService(
	IClientRepository clients,
	IDeletionLog log,
	ISettingsService settings,
	IToastService toasts,
	ILocalizer localizer,
	ILogger<ClientDeletionService> logger)
{
	public async Task DeleteAsync(Guid clientId)
	{
		var deleted = await clients.DeleteAsync(clientId);
		if (deleted is null)
			return;

		// The delete is done and recorded in the database. If the backup folder cannot be written right now, the entry is added
		// to the file later by SyncLogAsync, so tell the user instead of failing.
		try
		{
			await log.AppendAsync(settings.Current.EffectiveBackupFolder, [deleted]);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			logger.LogWarning(ex, "Could not write the deletion of client {ClientId} to the deletion log", clientId);
			toasts.Warning(localizer["Toast_DeletionLogFailed"]);
		}
	}

	/// <summary>
	/// Makes sure the log file holds every deletion the database knows about. Runs at startup, after a backup and when the backup
	/// folder changes, so a deletion that could not be written earlier (folder unreachable) or a new folder gets complete.
	/// </summary>
	public async Task SyncLogAsync()
	{
		try
		{
			var deletions = await clients.GetDeletionsAsync();
			await log.AppendAsync(settings.Current.EffectiveBackupFolder, deletions);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			logger.LogWarning(ex, "Could not bring the deletion log up to date");
		}
	}
}
