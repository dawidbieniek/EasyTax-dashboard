using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Limits;
using EasyTax.Core.Domain.Months;

namespace EasyTax.Core.Domain.Alerts;

/// <summary>
/// Works out which alerts a client has as of a given day. Only "now" counts: limits are judged for the calendar year of
/// <c>today</c> and a relief against <c>today</c>, so the year selected in the UI never matters. Suspended clients have none.
/// </summary>
public static class AlertEvaluator
{
	/// <param name="months">The client's month records; records outside <c>today</c>'s year are ignored.</param>
	public static IReadOnlyList<ClientAlert> Evaluate(Client client, IReadOnlyCollection<MonthRecord> months, DateOnly today, AlertOptions options)
	{
		if (client.IsSuspended)
			return [];

		options = options.Normalized();
		var alerts = new List<ClientAlert>();

		foreach (var status in LimitCalculator.CalculateAll(client, months, today.Year, options.LimitWarningThreshold))
		{
			var alert = FromLimit(client, status, today.Year);
			if (alert is not null)
				alerts.Add(alert);
		}

		var relief = FromRelief(client, today, options);
		if (relief is not null)
			alerts.Add(relief);

		return alerts;
	}

	private static ClientAlert? FromLimit(Client client, LimitStatus status, int year)
	{
		var severity = status.State switch
		{
			LimitState.Warning => AlertSeverity.Warning,
			LimitState.Over => AlertSeverity.Critical,
			_ => (AlertSeverity?)null,
		};
		if (severity is not { } found)
			return null;

		var kind = status.Type switch
		{
			LimitType.Vat => AlertKind.VatLimit,
			LimitType.CashRegister => AlertKind.CashRegisterLimit,
			_ => AlertKind.ZusPlusLimit,
		};

		return new(client, kind, found, year, Limit: status);
	}

	private static ClientAlert? FromRelief(Client client, DateOnly today, AlertOptions options)
	{
		if (client.ReliefEndDate is not { } end)
			return null;

		var daysLeft = end.DayNumber - today.DayNumber;
		if (daysLeft > options.ReliefWarningDays)
			return null;

		var severity = daysLeft < 0 ? AlertSeverity.Critical : AlertSeverity.Warning;

		return new(client, AlertKind.ReliefEnding, severity, end.Year, ReliefEndDate: end, DaysLeft: daysLeft);
	}
}
