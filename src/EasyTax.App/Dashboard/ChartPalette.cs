using EasyTax.App.Settings;
using SkiaSharp;
using Windows.UI.ViewManagement;

namespace EasyTax.App.Dashboard;

/// <summary>
/// Colours for the charts and gauges. They are drawn by SkiaSharp, not XAML, so they cannot use theme resources:
/// the palette is built from the Windows accent colour and the current light/dark theme.
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
	public static ChartPalette Create(AppTheme theme)
	{
		var settings = new UISettings();
		var dark = IsDark(theme, settings);
		// The same shades WinUI controls use: lighter on a dark background, darker on a light one, so the accent stays readable.
		var accent = ToSkia(settings.GetColorValue(dark ? UIColorType.AccentLight2 : UIColorType.AccentDark1));

		return dark
			? new ChartPalette(accent, new SKColor(108, 203, 95), new SKColor(252, 176, 64), new SKColor(255, 99, 99), new SKColor(205, 205, 205), new SKColor(255, 255, 255, 28), new SKColor(255, 255, 255, 40))
			: new ChartPalette(accent, new SKColor(16, 124, 16), new SKColor(214, 130, 0), new SKColor(196, 43, 28), new SKColor(70, 70, 70), new SKColor(0, 0, 0, 28), new SKColor(0, 0, 0, 30));
	}

	private static bool IsDark(AppTheme theme, UISettings settings)
	{
		if (theme != AppTheme.System)
			return theme == AppTheme.Dark;

		var background = settings.GetColorValue(UIColorType.Background);
		return background.R + background.G + background.B < 384;
	}

	private static SKColor ToSkia(Windows.UI.Color color) => new(color.R, color.G, color.B, color.A);
}
