using EasyTax.App.Settings;
using SkiaSharp;
using Windows.UI.ViewManagement;

namespace EasyTax.App.Dashboard;

/// <summary>
/// Colours for the charts and gauges. They are drawn by SkiaSharp, not XAML, so they cannot use theme resources:
/// the palette is built from the chosen accent colour and the current light/dark theme.
/// The status colours (green, amber, red) are the Material ones from App.xaml.
/// </summary>
public sealed record ChartPalette(
	SKColor Accent,
	SKColor Secondary,
	SKColor Warning,
	SKColor Danger,
	SKColor Label,
	SKColor Separator,
	SKColor Track)
{
	public static ChartPalette Create(AppTheme theme, AppAccent accentChoice)
	{
		var settings = new UISettings();
		var dark = IsDark(theme, settings);
		var accent = ToSkia(ResolveAccent(accentChoice, settings, dark));

		return dark
			? new ChartPalette(accent, new SKColor(0x66, 0xBB, 0x6A), new SKColor(0xFF, 0xCA, 0x28), new SKColor(0xEF, 0x53, 0x50), new SKColor(205, 205, 205), new SKColor(255, 255, 255, 28), new SKColor(255, 255, 255, 40))
			: new ChartPalette(accent, new SKColor(0x38, 0x8E, 0x3C), new SKColor(0xFF, 0x6F, 0x00), new SKColor(0xD3, 0x2F, 0x2F), new SKColor(70, 70, 70), new SKColor(0, 0, 0, 28), new SKColor(0, 0, 0, 30));
	}

	/// <summary>Whether the theme resolves to dark: the chosen one, or Windows' own when the app follows the system.</summary>
	public static bool IsDark(AppTheme theme, UISettings settings)
	{
		if (theme != AppTheme.System)
			return theme == AppTheme.Dark;

		var background = settings.GetColorValue(UIColorType.Background);
		return background.R + background.G + background.B < 384;
	}

	// The same shades WinUI controls use: lighter on a dark background, darker on a light one, so the accent stays readable.
	private static Windows.UI.Color ResolveAccent(AppAccent choice, UISettings settings, bool dark)
	{
		if (AppAccentColors.Of(choice) is { } picked)
			return AppAccentColors.Shade(picked, dark ? 0.4 : -0.2);

		return settings.GetColorValue(dark ? UIColorType.AccentLight2 : UIColorType.AccentDark1);
	}

	private static SKColor ToSkia(Windows.UI.Color color) => new(color.R, color.G, color.B, color.A);
}
