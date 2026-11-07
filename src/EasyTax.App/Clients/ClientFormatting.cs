using System.Globalization;

namespace EasyTax.App.Clients;

internal static class ClientFormatting
{
	/// <summary>Money and dates are always shown the Polish way, whatever the system culture is.</summary>
	public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("pl-PL");

	/// <summary>Shows a stored NIP (ten digits) as 000-000-00-00; anything else is returned unchanged.</summary>
	public static string Nip(string nip)
		=> nip.Length == 10 ? $"{nip[..3]}-{nip[3..6]}-{nip[6..8]}-{nip[8..]}" : nip;

	public static string Date(DateOnly? date) => date is { } value ? value.ToString("dd.MM.yyyy", Culture) : "—";

	public static string Money(decimal amount) => amount.ToString("C2", Culture);

	/// <summary>The text itself, or a dash when it is empty (for read-only display).</summary>
	public static string OrDash(string text) => string.IsNullOrWhiteSpace(text) ? "—" : text;
}
