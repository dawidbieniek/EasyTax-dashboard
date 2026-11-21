using System.Globalization;
using EasyTax.App.Clients;
using EasyTax.App.Localization;
using EasyTax.Core.Domain.Alerts;

namespace EasyTax.App.Alerts;

/// <summary>Turns an alert into the short texts the screens show (list tooltip, Alerts page).</summary>
public sealed class AlertDescriber(ILocalizer localizer)
{
	/// <summary>What the alert is about: "Limit VAT", "Koniec ulgi: Ulga na start", ...</summary>
	public string Title(ClientAlert alert) => alert.Kind switch
	{
		AlertKind.VatLimit => localizer["Alert_Vat_Title"],
		AlertKind.CashRegisterLimit => localizer["Alert_CashRegister_Title"],
		AlertKind.ZusPlusLimit => localizer["Alert_ZusPlus_Title"],
		_ => string.Format(CultureInfo.CurrentCulture, localizer["Alert_Relief_Title"], localizer.Label(alert.Client.ReliefType)),
	};

	/// <summary>The numbers: income against the limit, or when the relief ends.</summary>
	public string Detail(ClientAlert alert)
		=> alert.Limit is { } limit
			? string.Format(CultureInfo.CurrentCulture, localizer["Alert_Limit_Detail"], ClientFormatting.Money(limit.Income), ClientFormatting.Money(limit.Limit), Math.Round(limit.Ratio * 100).ToString("0", CultureInfo.CurrentCulture))
			: ReliefDetail(alert);

	/// <summary>Short urgency label.</summary>
	public string Severity(ClientAlert alert) => localizer[alert.Severity == AlertSeverity.Critical ? "Alert_Severity_Critical" : "Alert_Severity_Warning"];

	/// <summary>Title and detail on two lines, for tooltips.</summary>
	public string Summary(ClientAlert alert) => $"{Title(alert)}\n{Detail(alert)}";

	private string ReliefDetail(ClientAlert alert)
	{
		var date = ClientFormatting.Date(alert.ReliefEndDate);

		return alert.DaysLeft switch
		{
			< 0 => string.Format(CultureInfo.CurrentCulture, localizer["Alert_Relief_Ended"], date),
			0 => string.Format(CultureInfo.CurrentCulture, localizer["Alert_Relief_Today"], date),
			1 => string.Format(CultureInfo.CurrentCulture, localizer["Alert_Relief_Tomorrow"], date),
			var days => string.Format(CultureInfo.CurrentCulture, localizer["Alert_Relief_Soon"], date, days),
		};
	}
}
