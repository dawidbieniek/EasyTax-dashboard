using EasyTax.Core.Domain.Alerts;

namespace EasyTax.App.Alerts;

/// <summary>Writes to a client about an alert: fills the alert's template and opens the user's mail app. Nothing is sent by the app.</summary>
public interface IMailService
{
	/// <summary>
	/// Opens a new message to the client in the default mail app. Afterwards asks whether to mark the alert as handled,
	/// because opening a draft does not prove it was sent. Problems (no address, no mail app) are reported with a toast.
	/// </summary>
	Task WriteAsync(ClientAlert alert);
}
