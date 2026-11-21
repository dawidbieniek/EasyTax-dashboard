using EasyTax.Core.Domain.Alerts;

namespace EasyTax.App.Alerts;

/// <summary>One line of the Alerts page, with everything already formatted for display.</summary>
public sealed record AlertRow(
	AlertItem Item,
	Guid ClientId,
	string ClientName,
	byte[]? Photo,
	string Title,
	string Detail,
	string Severity,
	string ToggleHandledLabel,
	bool HasEmail,
	string EmailTooltip)
{
	public ClientAlert Alert => Item.Alert;

	public bool IsHandled => Item.IsHandled;

	public bool IsCritical => !IsHandled && Alert.Severity == AlertSeverity.Critical;

	public bool IsWarning => !IsHandled && Alert.Severity == AlertSeverity.Warning;

	/// <summary>Handled alerts are shown dimmed.</summary>
	public double RowOpacity => IsHandled ? 0.55 : 1;
}
