using EasyTax.Core.Common;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.Core.Domain.Months;

/// <summary>Everything recorded for one client in one month. At most one per client and month.</summary>
public class MonthRecord : Entity
{
	public required Guid ClientId { get; set; }

	public Client Client { get; set; } = null!;

	/// <summary>Always the first day of the month.</summary>
	public required DateOnly Month
	{
		get;
		set => field = value.FirstDayOfMonth;
	}

	public decimal Income { get; set; }

	public int SalesInvoices { get; set; }

	public int CostInvoices { get; set; }

	public bool IsAccounted { get; set; }

	// Checklist items that are tracked per month (null = not done)
	public DateTime? JpkV7DoneAt { get; set; }

	public DateTime? VatUeDoneAt { get; set; }
}
