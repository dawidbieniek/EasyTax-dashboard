namespace EasyTax.App.Settings;

/// <summary>User preferences and last-used selection, persisted to settings.json. Immutable: change it with <c>with</c> expressions.</summary>
public sealed record AppSettings
{
	public AppTheme Theme { get; init; } = AppTheme.Dark;

	// Selection restored on the next start (see ClientContext).
	public Guid? LastClientId { get; init; }

	public int? LastYear { get; init; }

	public int? LastMonth { get; init; }
}
