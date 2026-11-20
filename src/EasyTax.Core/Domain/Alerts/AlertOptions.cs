using EasyTax.Core.Domain.Limits;

namespace EasyTax.Core.Domain.Alerts;

/// <summary>User-configurable alert thresholds. Out-of-range values are brought back into range by <see cref="Normalized"/>.</summary>
/// <param name="LimitWarningPercent">Percent of an income limit at which a warning starts.</param>
/// <param name="ReliefWarningDays">Days before the end of a relief at which a warning starts.</param>
public sealed record AlertOptions(int LimitWarningPercent = AlertOptions.DefaultLimitWarningPercent, int ReliefWarningDays = AlertOptions.DefaultReliefWarningDays)
{
	public const int DefaultLimitWarningPercent = 90;
	public const int MinLimitWarningPercent = 50;
	public const int MaxLimitWarningPercent = 99;

	public const int DefaultReliefWarningDays = 7;
	public const int MinReliefWarningDays = 1;
	public const int MaxReliefWarningDays = 90;

	/// <summary>The limit warning as a fraction of the limit (0.9 for 90%), as <see cref="LimitStatus"/> expects it.</summary>
	public decimal LimitWarningThreshold => LimitWarningPercent / 100m;

	public AlertOptions Normalized() => new(
		Math.Clamp(LimitWarningPercent, MinLimitWarningPercent, MaxLimitWarningPercent),
		Math.Clamp(ReliefWarningDays, MinReliefWarningDays, MaxReliefWarningDays));
}
