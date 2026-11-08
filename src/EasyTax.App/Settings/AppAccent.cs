using Windows.UI;

namespace EasyTax.App.Settings;

/// <summary>The accent colours the user can pick: the Windows accent palette, or the colour Windows itself is set to.</summary>
public enum AppAccent
{
	System,
	Blue,
	Navy,
	Iris,
	Plum,
	Rose,
	Red,
	Orange,
	Teal,
	Green,
	Steel,
	Gray,
}

public static class AppAccentColors
{
	/// <summary>The palette colour of an accent. <see cref="AppAccent.System"/> has none: it follows Windows.</summary>
	public static Color? Of(AppAccent accent) => accent switch
	{
		AppAccent.Blue => Color.FromArgb(255, 0x00, 0x78, 0xD7),
		AppAccent.Navy => Color.FromArgb(255, 0x00, 0x63, 0xB1),
		AppAccent.Iris => Color.FromArgb(255, 0x87, 0x64, 0xB8),
		AppAccent.Plum => Color.FromArgb(255, 0xBF, 0x00, 0x77),
		AppAccent.Rose => Color.FromArgb(255, 0xC3, 0x00, 0x52),
		AppAccent.Red => Color.FromArgb(255, 0xE8, 0x11, 0x23),
		AppAccent.Orange => Color.FromArgb(255, 0xF7, 0x63, 0x0C),
		AppAccent.Teal => Color.FromArgb(255, 0x03, 0x83, 0x87),
		AppAccent.Green => Color.FromArgb(255, 0x10, 0x7C, 0x10),
		AppAccent.Steel => Color.FromArgb(255, 0x68, 0x76, 0x8A),
		AppAccent.Gray => Color.FromArgb(255, 0x7A, 0x75, 0x74),
		_ => null,
	};

	/// <summary>Mixes a colour towards white (positive amount) or black (negative), the way Windows derives its Light/Dark accent shades.</summary>
	public static Color Shade(Color color, double amount)
	{
		var target = amount >= 0 ? 255 : 0;
		var t = Math.Abs(amount);

		byte Mix(byte value) => (byte)Math.Round(value + (target - value) * t);

		return Color.FromArgb(color.A, Mix(color.R), Mix(color.G), Mix(color.B));
	}
}
