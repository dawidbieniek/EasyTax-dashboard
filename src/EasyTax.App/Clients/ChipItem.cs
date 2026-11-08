using Windows.UI;

namespace EasyTax.App.Clients;

/// <summary>A small tinted label on the client page. Each kind of fact has its own colour, so a glance tells what a client has.</summary>
public sealed record ChipItem(string Text, Color Color)
{
	// Material Design 400 shades: readable as a border and as a light tint over both the light and the dark background.
	public static readonly Color Blue = Color.FromArgb(255, 0x42, 0xA5, 0xF5);
	public static readonly Color Indigo = Color.FromArgb(255, 0x5C, 0x6B, 0xC0);
	public static readonly Color Teal = Color.FromArgb(255, 0x26, 0xA6, 0x9A);
	public static readonly Color Pink = Color.FromArgb(255, 0xEC, 0x40, 0x7A);
	public static readonly Color Purple = Color.FromArgb(255, 0xAB, 0x47, 0xBC);
	public static readonly Color BlueGrey = Color.FromArgb(255, 0x78, 0x90, 0x9C);
	public static readonly Color Green = Color.FromArgb(255, 0x66, 0xBB, 0x6A);
	public static readonly Color Amber = Color.FromArgb(255, 0xFF, 0xCA, 0x28);

	public Color Fill => Color.FromArgb(40, Color.R, Color.G, Color.B);

	public Color Stroke => Color.FromArgb(170, Color.R, Color.G, Color.B);
}
