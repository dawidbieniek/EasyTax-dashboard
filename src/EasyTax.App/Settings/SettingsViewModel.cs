using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyTax.App.Localization;
using EasyTax.App.Shell;

namespace EasyTax.App.Settings;

public sealed partial class SettingsViewModel : ViewModelBase
{
	private readonly ISettingsService _settings;

	public SettingsViewModel(ISettingsService settings, ILocalizer localizer)
	{
		_settings = settings;

		ThemeOptions =
		[
			new(AppTheme.System, localizer["Theme_System"]),
			new(AppTheme.Light, localizer["Theme_Light"]),
			new(AppTheme.Dark, localizer["Theme_Dark"]),
		];
		SelectedTheme = ThemeOptions.First(o => o.Value == settings.Current.Theme);

		AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
	}

	public IReadOnlyList<ThemeOption> ThemeOptions { get; }

	public string AppVersion { get; }

	[ObservableProperty]
	public partial ThemeOption SelectedTheme { get; set; }

	partial void OnSelectedThemeChanged(ThemeOption value)
		=> _settings.Update(s => s with { Theme = value.Value });
}

public sealed record ThemeOption(AppTheme Value, string Label);
