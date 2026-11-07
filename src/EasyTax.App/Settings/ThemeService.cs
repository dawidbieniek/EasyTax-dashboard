using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace EasyTax.App.Settings;

public sealed class ThemeService(ISettingsService settings) : IThemeService
{
	private Window? _window;

	public void Attach(Window window)
	{
		_window = window;
		settings.Changed += (_, _) => Apply();
		Apply();
	}

	private void Apply()
	{
		if (_window?.Content is not FrameworkElement root)
			return;

		var theme = settings.Current.Theme;

		root.RequestedTheme = theme switch
		{
			AppTheme.Light => ElementTheme.Light,
			AppTheme.Dark => ElementTheme.Dark,
			_ => ElementTheme.Default,
		};

		// Keeps the caption buttons readable on the extended title bar.
		_window.AppWindow.TitleBar.PreferredTheme = theme switch
		{
			AppTheme.Light => TitleBarTheme.Light,
			AppTheme.Dark => TitleBarTheme.Dark,
			_ => TitleBarTheme.UseDefaultAppMode,
		};
	}
}
