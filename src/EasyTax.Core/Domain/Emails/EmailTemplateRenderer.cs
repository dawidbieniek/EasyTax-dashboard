using System.Globalization;
using System.Text.RegularExpressions;
using EasyTax.Core.Domain.Alerts;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.Core.Domain.Emails;

/// <summary>
/// Fills the <c>{tokens}</c> of a template with the client's data and the numbers of the alert. A token that does not exist
/// is left as written (so a typo is noticed); a token that has no value for this alert (an income token on a relief alert)
/// becomes empty.
/// </summary>
public static partial class EmailTemplateRenderer
{
	private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

	/// <summary>The tokens a template may use, in the order the settings list shows them.</summary>
	public static IReadOnlyList<string> TokenNames { get; } =
	[
		"zwrot", "panPani", "panaPani", "panuPani", "panaPania", "imie", "nazwisko", "nip", "rok",
		"dochod", "limit", "procent", "pozostalo", "ulga", "dataKoniecUlgi", "dniDoKoncaUlgi",
	];

	public static string Render(string template, ClientAlert alert)
	{
		var tokens = Tokens(alert);
		return TokenPattern().Replace(template, match => tokens.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
	}

	public static Dictionary<string, string> Tokens(ClientAlert alert)
	{
		var client = alert.Client;
		var female = client.Gender == Gender.Female;
		var limit = alert.Limit;

		return new(StringComparer.Ordinal)
		{
			// The five forms of "Pan / Pani" the sentences need: who is addressed, whose, to whom, whom.
			["zwrot"] = female ? "Szanowna Pani" : "Szanowny Panie",
			["panPani"] = female ? "Pani" : "Pan",
			["panaPani"] = female ? "Pani" : "Pana",
			["panuPani"] = female ? "Pani" : "Panu",
			["panaPania"] = female ? "Panią" : "Pana",
			["imie"] = client.FirstName,
			["nazwisko"] = client.LastName,
			["nip"] = client.Nip,
			["rok"] = alert.Year.ToString(Polish),
			["dochod"] = limit is null ? string.Empty : Money(limit.Income),
			["limit"] = limit is null ? string.Empty : Money(limit.Limit),
			["procent"] = limit is null ? string.Empty : Math.Round(limit.Ratio * 100).ToString("0", Polish),
			["pozostalo"] = limit is null ? string.Empty : Money(Math.Max(limit.Limit - limit.Income, 0)),
			["ulga"] = ReliefName(client.ReliefType),
			["dataKoniecUlgi"] = alert.ReliefEndDate?.ToString("dd.MM.yyyy", Polish) ?? string.Empty,
			["dniDoKoncaUlgi"] = alert.DaysLeft is { } days ? Math.Max(days, 0).ToString(Polish) : string.Empty,
		};
	}

	// Mail text should not carry the non-breaking spaces the Polish currency format uses.
	private static string Money(decimal amount) => amount.ToString("C2", Polish).Replace(' ', ' ').Replace(' ', ' ');

	private static string ReliefName(ReliefType type) => type switch
	{
		ReliefType.Start => "Ulga na start",
		ReliefType.PreferentialZus => "ZUS preferencyjny",
		ReliefType.ZusPlus => "Mały ZUS Plus",
		_ => "Pełny ZUS",
	};

	[GeneratedRegex(@"\{(\w+)\}")]
	private static partial Regex TokenPattern();
}
