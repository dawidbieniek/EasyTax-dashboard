namespace EasyTax.Core.Domain.Limits;

/// <summary>Where a client stands against one income limit in one year.</summary>
/// <param name="Type">Which limit.</param>
/// <param name="IsApplicable">False when the limit does not concern the client (no cash register, not on Mały ZUS Plus).</param>
/// <param name="Income">Income counted towards the limit.</param>
/// <param name="Limit">The limit for that year (prorated for the VAT limit in the join year).</param>
/// <param name="CountedFrom">First day income is counted from.</param>
/// <param name="CountedTo">Last day income is counted to.</param>
public sealed record LimitStatus(LimitType Type, bool IsApplicable, decimal Income, decimal Limit, DateOnly CountedFrom, DateOnly CountedTo)
{
	/// <summary>Income divided by the limit (1 = exactly at the limit).</summary>
	public decimal Ratio => Limit > 0 ? Income / Limit : 0;

	public LimitState State
	{
		get
		{
			if (!IsApplicable || Limit <= 0)
				return LimitState.Ok;

			if (Income >= Limit)
				return LimitState.Over;

			return Income >= Limit * LimitCalculator.WarningThreshold
				? LimitState.Warning
				: LimitState.Ok;
		}
	}

	public static LimitStatus NotApplicable(LimitType type, decimal limit, DateOnly countedFrom, DateOnly countedTo)
		=> new(type, false, 0, limit, countedFrom, countedTo);
}
