using EasyTax.Core.Common;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;

namespace EasyTax.Core.Domain.Limits;

/// <summary>
/// Income limits for a client in a given year. All three work on the same input: the client plus the month records
/// of that year.
/// </summary>
public static class LimitCalculator
{
	public const decimal VatBaseLimit = 200_000m;
	public const decimal CashRegisterLimit = 20_000m;
	public const decimal ZusPlusLimit = 120_000m;

	/// <summary>Fraction of a limit at which the status turns to a warning.</summary>
	public const decimal WarningThreshold = 0.9m;

	public static IReadOnlyList<LimitStatus> CalculateAll(Client client, IReadOnlyCollection<MonthRecord> months, int year)
		=> [CalculateVat(client, months, year), CalculateCashRegister(client, months, year), CalculateZusPlus(client, months, year)];

	/// <summary>
	/// VAT exemption limit. In the join year it is counted from the join month and the limit is prorated
	/// by the days left in the year from the join date.
	/// </summary>
	public static LimitStatus CalculateVat(Client client, IEnumerable<MonthRecord> months, int year)
	{
		var yearStart = new DateOnly(year, 1, 1);
		var yearEnd = new DateOnly(year, 12, 31);
		var isJoinYear = client.JoinDate.Year == year;

		var countedFrom = isJoinYear ? client.JoinMonth : yearStart;
		var limit = isJoinYear
			? Math.Round(VatBaseLimit * client.JoinDate.DaysUntilEndOfYear / client.JoinDate.DaysInYear, 2)
			: VatBaseLimit;

		return new(LimitType.Vat, true, SumIncome(months, countedFrom, yearEnd), limit, countedFrom, yearEnd);
	}

	/// <summary>Cash register limit. Counted from the month of the first fiscal transaction; only for clients who use a cash register.</summary>
	public static LimitStatus CalculateCashRegister(Client client, IEnumerable<MonthRecord> months, int year)
	{
		var yearStart = new DateOnly(year, 1, 1);
		var yearEnd = new DateOnly(year, 12, 31);

		if (!client.UsesCashRegister || client.FirstCashRegisterDate is not { } firstUse)
			return LimitStatus.NotApplicable(LimitType.CashRegister, CashRegisterLimit, yearStart, yearEnd);

		// Years before the first use start after their own end, so nothing is counted.
		var countedFrom = firstUse.Year >= year ? firstUse.FirstDayOfMonth : yearStart;

		return new(LimitType.CashRegister, true, SumIncome(months, countedFrom, yearEnd), CashRegisterLimit, countedFrom, yearEnd);
	}

	/// <summary>Mały ZUS Plus limit. Whole calendar year; only for clients on that relief with a known start date.</summary>
	public static LimitStatus CalculateZusPlus(Client client, IEnumerable<MonthRecord> months, int year)
	{
		var yearStart = new DateOnly(year, 1, 1);
		var yearEnd = new DateOnly(year, 12, 31);

		if (client.ReliefType != ReliefType.ZusPlus || client.ReliefSinceDate is not { } since)
			return LimitStatus.NotApplicable(LimitType.ZusPlus, ZusPlusLimit, yearStart, yearEnd);

		var countedFrom = since.Year > year ? since.FirstDayOfYear : yearStart;

		return new(LimitType.ZusPlus, true, SumIncome(months, countedFrom, yearEnd), ZusPlusLimit, countedFrom, yearEnd);
	}

	private static decimal SumIncome(IEnumerable<MonthRecord> months, DateOnly from, DateOnly to)
		=> months.Where(m => m.Month >= from && m.Month <= to).Sum(m => m.Income);
}
