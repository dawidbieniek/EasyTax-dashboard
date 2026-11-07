namespace EasyTax.Data.Banks;

/// <summary>Polish banks offered when assigning a client to a bank. Missing ones are added at startup.</summary>
internal static class BankSeed
{
	public static IReadOnlyList<string> Names { get; } =
	[
		"Aion Bank",
		"Alior Bank",
		"Bank BNP Paribas",
		"Bank BPH S.A.",
		"Bank BPS",
		"Bank Millennium",
		"Bank Nowy",
		"Bank Pocztowy",
		"Bank Pekao S.A.",
		"Bank Śląski",
		"BOŚ Bank",
		"Citi Bank Handlowy",
		"Credit Agricole",
		"Deutsche Bank Polska S.A.",
		"DNB Bank Polska",
		"Ikano Bank",
		"InBank",
		"ING Bank Śląski",
		"mBank",
		"Nest Bank S.A.",
		"PKO Bank Polski",
		"Plus Bank",
		"Santander Bank Polska",
		"Santander Consumer Bank",
		"SGB Bank",
		"Toyota Bank Polska",
		"Velo Bank",
	];
}
