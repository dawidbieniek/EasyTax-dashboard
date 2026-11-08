using EasyTax.App.Dashboard;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace EasyTax.App.Settings;

public sealed class ThemeService(ISettingsService settings) : IThemeService
{
	private static readonly string[] AccentKeys =
	[
		"SystemAccentColor",
		"SystemAccentColorLight1",
		"SystemAccentColorLight2",
		"SystemAccentColorLight3",
		"SystemAccentColorDark1",
		"SystemAccentColorDark2",
		"SystemAccentColorDark3",
	];

	private Window? _window;
	private AppAccent? _appliedAccent;

	public void Attach(Window window)
	{
		_window = window;
		settings.Changed += (_, _) => Apply();
		Apply();
	}

	public static ElementTheme ToElementTheme(AppTheme theme) => theme switch
	{
		AppTheme.Light => ElementTheme.Light,
		AppTheme.Dark => ElementTheme.Dark,
		_ => ElementTheme.Default,
	};

	private void Apply()
	{
		if (_window?.Content is not FrameworkElement root)
			return;

		var theme = settings.Current.Theme;
		var target = ToElementTheme(theme);

		var accentChanged = ApplyAccent(settings.Current.Accent);

		// Controls read the accent brushes through theme resources, which only look the values up again when the theme changes.
		if (accentChanged)
			root.RequestedTheme = target == ElementTheme.Dark ? ElementTheme.Light : ElementTheme.Dark;

		root.RequestedTheme = target;

		var titleBar = _window.AppWindow.TitleBar;
		var dark = ChartPalette.IsDark(theme, new UISettings());

		// Keeps the caption buttons readable on the extended title bar.
		titleBar.PreferredTheme = theme switch
		{
			AppTheme.Light => TitleBarTheme.Light,
			AppTheme.Dark => TitleBarTheme.Dark,
			_ => TitleBarTheme.UseDefaultAppMode,
		};

		// Windows picks the caption button colours from its own theme; with a different theme chosen in the app they were barely visible.
		var glyph = dark ? Colors.White : Color.FromArgb(255, 26, 26, 26);
		var inactive = dark ? Color.FromArgb(255, 150, 150, 150) : Color.FromArgb(255, 120, 120, 120);
		var hover = dark ? Color.FromArgb(24, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0);
		var pressed = dark ? Color.FromArgb(14, 255, 255, 255) : Color.FromArgb(10, 0, 0, 0);

		titleBar.ButtonBackgroundColor = Colors.Transparent;
		titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		titleBar.ButtonForegroundColor = glyph;
		titleBar.ButtonInactiveForegroundColor = inactive;
		titleBar.ButtonHoverForegroundColor = glyph;
		titleBar.ButtonHoverBackgroundColor = hover;
		titleBar.ButtonPressedForegroundColor = glyph;
		titleBar.ButtonPressedBackgroundColor = pressed;
	}

	/// <summary>Overrides the system accent resources with the chosen colour (or removes the override). True when anything changed.</summary>
	private bool ApplyAccent(AppAccent accent)
	{
		if (_appliedAccent == accent)
			return false;

		var firstRun = _appliedAccent is null;
		_appliedAccent = accent;

		var resources = Application.Current.Resources;
		var color = AppAccentColors.Of(accent);

		if (color is not { } picked)
		{
			foreach (var key in AccentKeys)
				resources.Remove(key);

			// Nothing was overridden on the first run, so there is nothing to refresh either.
			return !firstRun;
		}

		resources["SystemAccentColor"] = picked;
		resources["SystemAccentColorLight1"] = AppAccentColors.Shade(picked, 0.2);
		resources["SystemAccentColorLight2"] = AppAccentColors.Shade(picked, 0.4);
		resources["SystemAccentColorLight3"] = AppAccentColors.Shade(picked, 0.6);
		resources["SystemAccentColorDark1"] = AppAccentColors.Shade(picked, -0.2);
		resources["SystemAccentColorDark2"] = AppAccentColors.Shade(picked, -0.4);
		resources["SystemAccentColorDark3"] = AppAccentColors.Shade(picked, -0.6);

		return true;
	}
}
