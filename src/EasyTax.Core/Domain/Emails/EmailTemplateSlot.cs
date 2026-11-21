using EasyTax.Core.Domain.Alerts;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.Core.Domain.Emails;

/// <summary>
/// One place that needs an email template: an alert kind at a severity (a relief alert also per relief type, because
/// each relief has its own wording).
/// </summary>
public sealed record EmailTemplateSlot(AlertKind Kind, AlertSeverity Severity, ReliefType? Relief = null)
{
	/// <summary>Stable text key stored with the template: <c>VatLimit.Warning</c>, <c>ReliefEnding.Start.Critical</c>.</summary>
	public string Key => Relief is { } relief ? $"{Kind}.{relief}.{Severity}" : $"{Kind}.{Severity}";

	/// <summary>Every slot, in the order the settings list shows them.</summary>
	public static IReadOnlyList<EmailTemplateSlot> All { get; } =
	[
		.. new[] { AlertKind.VatLimit, AlertKind.CashRegisterLimit, AlertKind.ZusPlusLimit }.SelectMany(kind => Severities(kind, null)),
		.. new[] { ReliefType.Start, ReliefType.PreferentialZus, ReliefType.ZusPlus }.SelectMany(relief => Severities(AlertKind.ReliefEnding, relief)),
	];

	public static EmailTemplateSlot For(ClientAlert alert)
		=> new(alert.Kind, alert.Severity, alert.Kind == AlertKind.ReliefEnding ? alert.Client.ReliefType : null);

	private static IEnumerable<EmailTemplateSlot> Severities(AlertKind kind, ReliefType? relief)
		=> [new(kind, AlertSeverity.Warning, relief), new(kind, AlertSeverity.Critical, relief)];
}
