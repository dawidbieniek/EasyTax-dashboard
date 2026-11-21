using System.Text.Json.Serialization;
using EasyTax.App.Infrastructure;
using EasyTax.Core.Domain.Alerts;

namespace EasyTax.App.Settings;

/// <summary>User preferences and last-used selection, persisted to settings.json. Immutable: change it with <c>with</c> expressions.</summary>
public sealed record AppSettings
{
	public AppTheme Theme { get; init; } = AppTheme.Dark;

	public AppAccent Accent { get; init; } = AppAccent.System;

	/// <summary>Where backups are written. Null means the default (Documents\EasyTax\Backups).</summary>
	public string? BackupFolder { get; init; }

	/// <summary>The folder backups really go to: the chosen one, else the default.</summary>
	[JsonIgnore]
	public string EffectiveBackupFolder => BackupFolder is { Length: > 0 } chosen ? chosen : AppPaths.DefaultBackupFolder;

	/// <summary>Make a backup at startup when the newest one is older than seven days.</summary>
	public bool ScheduledBackups { get; init; } = true;

	/// <summary>Percent of an income limit at which a client gets a warning (gauges and alerts). Kept within the allowed range.</summary>
	public int LimitWarningPercent
	{
		get;
		init => field = Math.Clamp(value, AlertOptions.MinLimitWarningPercent, AlertOptions.MaxLimitWarningPercent);
	} = AlertOptions.DefaultLimitWarningPercent;

	/// <summary>Days before the end of a relief at which a client gets a warning. Kept within the allowed range.</summary>
	public int ReliefWarningDays
	{
		get;
		init => field = Math.Clamp(value, AlertOptions.MinReliefWarningDays, AlertOptions.MaxReliefWarningDays);
	} = AlertOptions.DefaultReliefWarningDays;

	/// <summary>The alert thresholds as the Core rules expect them.</summary>
	[JsonIgnore]
	public AlertOptions Alerts => new(LimitWarningPercent, ReliefWarningDays);

	/// <summary>The newest app version whose "what's new" window was shown (null before the first one). Compared with the running version at startup.</summary>
	public string? LastSeenVersion { get; init; }

	// Selection restored on the next start (see ClientContext).
	public Guid? LastClientId { get; init; }

	public int? LastYear { get; init; }

	public int? LastMonth { get; init; }
}
