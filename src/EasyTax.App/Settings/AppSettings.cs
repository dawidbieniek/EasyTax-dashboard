using System.Text.Json.Serialization;
using EasyTax.App.Infrastructure;

namespace EasyTax.App.Settings;

/// <summary>User preferences and last-used selection, persisted to settings.json. Immutable: change it with <c>with</c> expressions.</summary>
public sealed record AppSettings
{
	public AppTheme Theme { get; init; } = AppTheme.Dark;

	/// <summary>Where backups are written. Null means the default (Documents\EasyTax\Backups).</summary>
	public string? BackupFolder { get; init; }

	/// <summary>The folder backups really go to: the chosen one, else the default.</summary>
	[JsonIgnore]
	public string EffectiveBackupFolder => BackupFolder is { Length: > 0 } chosen ? chosen : AppPaths.DefaultBackupFolder;

	/// <summary>Make a backup at startup when the newest one is older than seven days.</summary>
	public bool ScheduledBackups { get; init; } = true;

	// Selection restored on the next start (see ClientContext).
	public Guid? LastClientId { get; init; }

	public int? LastYear { get; init; }

	public int? LastMonth { get; init; }
}
