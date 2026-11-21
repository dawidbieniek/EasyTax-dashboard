using EasyTax.Core.Common;

namespace EasyTax.Core.Domain.Alerts;

/// <summary>
/// "I already told this client": silences one alert until it gets worse. One per client, alert kind and year.
/// </summary>
public class AlertAcknowledgement : Entity
{
	public required Guid ClientId { get; set; }

	public required AlertKind Kind { get; set; }

	/// <summary>The <see cref="ClientAlert.Year"/> the acknowledgement belongs to; a new year starts clean.</summary>
	public required int Year { get; set; }

	/// <summary>How severe the alert was when it was acknowledged.</summary>
	public required AlertSeverity Level { get; set; }

	public DateTime AcknowledgedAt { get; set; } = DateTime.UtcNow;

	/// <summary>True when this acknowledgement silences the alert: same client, kind and year, and the alert is not worse than it was.</summary>
	public bool Covers(ClientAlert alert)
		=> ClientId == alert.Client.Id && Kind == alert.Kind && Year == alert.Year && alert.Severity <= Level;
}
