using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Limits;

namespace EasyTax.Core.Domain.Alerts;

/// <summary>One thing that needs a client's attention. Computed on demand, never stored.</summary>
/// <param name="Client">The client the alert is about.</param>
/// <param name="Kind">What the alert is about.</param>
/// <param name="Severity">How urgent it is.</param>
/// <param name="Year">Year the alert belongs to: the calendar year for limits, the year of the end date for a relief. Acknowledgements are kept per year.</param>
/// <param name="Limit">Income, limit and period for the limit kinds; null for a relief alert.</param>
/// <param name="ReliefEndDate">Last day of the relief for a relief alert; null for the limit kinds.</param>
/// <param name="DaysLeft">Days from today to <paramref name="ReliefEndDate"/>; zero or negative once it has passed. Null for the limit kinds.</param>
public sealed record ClientAlert(
	Client Client,
	AlertKind Kind,
	AlertSeverity Severity,
	int Year,
	LimitStatus? Limit = null,
	DateOnly? ReliefEndDate = null,
	int? DaysLeft = null);
