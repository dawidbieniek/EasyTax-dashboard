namespace EasyTax.Core.Domain.Alerts;

/// <summary>Ordered by urgency: a higher value is worse.</summary>
public enum AlertSeverity
{
	/// <summary>Limit nearly reached, or the relief ends soon.</summary>
	Warning,

	/// <summary>Limit reached or exceeded, or the relief has ended.</summary>
	Critical,
}
