namespace EasyTax.App.Settings;

/// <summary>User preferences persisted to settings.json. Immutable: change it with <c>with</c> expressions.</summary>
public sealed record AppSettings
{
	public AppTheme Theme { get; init; } = AppTheme.Dark;
}
