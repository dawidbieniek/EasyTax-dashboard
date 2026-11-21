using EasyTax.Core.Domain.Alerts;

namespace EasyTax.App.Alerts;

/// <summary>
/// The alerts of every active client as of today. Evaluated on demand from the database (nothing is stored but the
/// "already told them" marks) and refreshed shortly after the data or the thresholds change.
/// </summary>
public interface IAlertService
{
	/// <summary>All alerts, handled ones included, most urgent first, then by client name.</summary>
	IReadOnlyList<AlertItem> Items { get; }

	/// <summary>Number of alerts that are not handled yet (the badge on the navigation rail).</summary>
	int ActiveCount { get; }

	/// <summary>Raised when the list really changed, not after every refresh.</summary>
	event EventHandler? Changed;

	/// <summary>Evaluates everything now. Safe to call at any time; a failure is logged and the old list stays.</summary>
	Task RefreshAsync();

	Task AcknowledgeAsync(ClientAlert alert);

	/// <summary>Takes the "already told" mark back, so the alert is active again.</summary>
	Task UndoAcknowledgeAsync(ClientAlert alert);
}
