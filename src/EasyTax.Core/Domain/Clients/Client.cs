using EasyTax.Core.Common;
using EasyTax.Core.Domain.Banks;
using EasyTax.Core.Domain.Months;

namespace EasyTax.Core.Domain.Clients;

/// <summary>A client of the accounting office.</summary>
public class Client : Entity
{
	private const int StartReliefMonths = 6;
	private const int PreferentialReliefYears = 2;
	private const int ZusPlusReliefYears = 3;

	// Personal
	public required string FirstName { get; set; }

	public required string LastName { get; set; }

	public Gender Gender { get; set; } = Gender.Male;

	/// <summary>Digits only (see <see cref="ClientValidation.NormalizeNip"/>).</summary>
	public required string Nip { get; set; }

	public string PhoneNumber { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public required DateOnly JoinDate { get; set; }

	public required Guid BankId { get; set; }

	public Bank Bank { get; set; } = null!;

	public bool IsSuspended { get; set; }

	public string Notes { get; set; } = string.Empty;

	// Subscription
	public string PlanName { get; set; } = string.Empty;

	public decimal AuthorizationFee { get; set; }

	public decimal SubscriptionFee { get; set; }

	public bool EmploymentContract { get; set; }

	public bool CashMethod { get; set; }

	// Taxes
	public TaxType TaxType { get; set; } = TaxType.Scale;

	public PaymentFrequency PitPaymentFrequency { get; set; } = PaymentFrequency.Monthly;

	public bool IsVatPayer { get; set; }

	public PaymentFrequency VatPaymentFrequency { get; set; } = PaymentFrequency.Monthly;

	public bool UsesCashRegister { get; set; }

	public DateOnly? FirstCashRegisterDate { get; set; }

	// Relief
	public ReliefType ReliefType { get; set; } = ReliefType.Start;

	/// <summary>When the client moved to the current relief. Only meaningful for Mały ZUS Plus.</summary>
	public DateOnly? ReliefSinceDate { get; set; }

	// Checklist items that are tracked per client (null = not done)
	public DateTime? VatRDoneAt { get; set; }

	public DateTime? Ceidg1DoneAt { get; set; }

	public DateTime? ZusDoneAt { get; set; }

	public DateTime? InvoicedAt { get; set; }

	public DateTime? ZusDraDoneAt { get; set; }

	public DateTime? TaxDoneAt { get; set; }

	// Related data
	public ClientPhoto? Photo { get; set; }

	public List<MonthRecord> Months { get; set; } = [];

	public string FullName => $"{FirstName} {LastName}";

	/// <summary>First day of the month the client joined. Earlier months are not selectable.</summary>
	public DateOnly JoinMonth => JoinDate.FirstDayOfMonth;

	/// <summary>
	/// Last day of the current relief, or null when the relief does not expire (or lacks the date it is counted from).
	/// Ulga na start and ZUS preferencyjny count from the join date, Mały ZUS Plus from <see cref="ReliefSinceDate"/>.
	/// A relief that starts mid-month runs one month longer.
	/// </summary>
	public DateOnly? ReliefEndDate
	{
		get
		{
			var reference = ReliefType == ReliefType.ZusPlus ? ReliefSinceDate : JoinDate;
			if (reference is not { } date)
				return null;

			var firstOfMonth = date.FirstDayOfMonth;
			var offset = date.Day == 1 ? 0 : 1;

			return ReliefType switch
			{
				ReliefType.Start => firstOfMonth.AddMonths(StartReliefMonths + offset).AddDays(-1),
				ReliefType.PreferentialZus => firstOfMonth.AddYears(PreferentialReliefYears).AddMonths(StartReliefMonths + offset).AddDays(-1),
				ReliefType.ZusPlus => firstOfMonth.AddYears(ZusPlusReliefYears).AddMonths(offset).AddDays(-1),
				_ => null,
			};
		}
	}

	public bool IsMonthSelectable(DateOnly month) => month.FirstDayOfMonth >= JoinMonth;

	public MonthState GetMonthState(DateOnly month, IEnumerable<MonthRecord> months)
	{
		var firstOfMonth = month.FirstDayOfMonth;
		if (!IsMonthSelectable(firstOfMonth))
			return MonthState.BeforeJoin;

		return months.Any(m => m.Month == firstOfMonth && m.IsAccounted)
			? MonthState.Accounted
			: MonthState.NotAccounted;
	}
}
