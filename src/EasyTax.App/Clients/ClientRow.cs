using EasyTax.Core.Domain.Months;

namespace EasyTax.App.Clients;

/// <summary>One line of the clients list, with everything already formatted for display.</summary>
public sealed record ClientRow(
	Guid Id,
	string FullName,
	string Nip,
	string BankName,
	string TaxType,
	string Relief,
	string Phone,
	string Email,
	MonthState MonthState,
	string MonthStatus,
	bool IsSuspended,
	string SuspendLabel,
	byte[]? Photo)
{
	/// <summary>Suspended clients are shown dimmed.</summary>
	public double RowOpacity => IsSuspended ? 0.5 : 1;
}
