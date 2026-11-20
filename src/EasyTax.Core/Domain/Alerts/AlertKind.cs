namespace EasyTax.Core.Domain.Alerts;

public enum AlertKind
{
	/// <summary>Income towards the VAT exemption limit.</summary>
	VatLimit,

	/// <summary>Income towards the cash register limit.</summary>
	CashRegisterLimit,

	/// <summary>Income towards the Mały ZUS Plus limit.</summary>
	ZusPlusLimit,

	/// <summary>The current relief (Ulga na start, ZUS preferencyjny, Mały ZUS Plus) is about to end or has ended.</summary>
	ReliefEnding,
}
