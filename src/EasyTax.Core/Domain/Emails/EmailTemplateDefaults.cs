using EasyTax.Core.Domain.Alerts;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.Core.Domain.Emails;

/// <summary>The wording shipped with the app. Missing templates are created from it at startup; "restore default" in settings returns to it.</summary>
public static class EmailTemplateDefaults
{
	private const string Closing = "\n\nPozdrawiam";

	public static (string Subject, string Body) For(EmailTemplateSlot slot)
	{
		var critical = slot.Severity == AlertSeverity.Critical;

		return slot.Kind switch
		{
			AlertKind.VatLimit => critical
				? ("Przekroczono limit zwolnienia z VAT", "{zwrot},\n\ninformuję, że przychód {panaPani} w {rok} roku wynosi {dochod} i przekroczył limit zwolnienia z VAT ({limit}).\n\nProszę o pilny kontakt, aby omówić rejestrację jako podatnik VAT." + Closing)
				: ("Zbliża się limit zwolnienia z VAT", "{zwrot},\n\ninformuję, że przychód {panaPani} w {rok} roku wynosi już {dochod}. To {procent}% limitu zwolnienia z VAT ({limit}); do limitu pozostało {pozostalo}.\n\nPo jego przekroczeniu konieczna będzie rejestracja jako podatnik VAT. Proszę o kontakt, aby omówić dalsze kroki." + Closing),

			AlertKind.CashRegisterLimit => critical
				? ("Przekroczono limit dla kasy fiskalnej", "{zwrot},\n\ninformuję, że przychód {panaPani} w {rok} roku wynosi {dochod} i przekroczył limit dla kasy fiskalnej ({limit}).\n\nProszę o pilny kontakt, aby omówić dalsze kroki." + Closing)
				: ("Zbliża się limit dla kasy fiskalnej", "{zwrot},\n\ninformuję, że przychód {panaPani} w {rok} roku wynosi {dochod}, czyli {procent}% limitu dla kasy fiskalnej ({limit}). Do limitu pozostało {pozostalo}.\n\nProszę o kontakt, aby omówić dalsze kroki." + Closing),

			AlertKind.ZusPlusLimit => critical
				? ("Przekroczono limit dla Małego ZUS Plus", "{zwrot},\n\ninformuję, że przychód {panaPani} w {rok} roku wynosi {dochod} i przekroczył limit dla Małego ZUS Plus ({limit}).\n\nProszę o pilny kontakt w sprawie dalszych kroków." + Closing)
				: ("Zbliża się limit dla Małego ZUS Plus", "{zwrot},\n\ninformuję, że przychód {panaPani} w {rok} roku wynosi {dochod}, czyli {procent}% limitu dla Małego ZUS Plus ({limit}). Po jego przekroczeniu {panPani} może stracić prawo do tej ulgi.\n\nProszę o kontakt, aby omówić dalsze kroki." + Closing),

			_ => critical
				? ("Koniec ulgi ZUS ({ulga})", "{zwrot},\n\ninformuję, że okres preferencji ZUS ({ulga}) przysługujący {panuPani} zakończył się {dataKoniecUlgi}. Od tego czasu obowiązują wyższe składki ZUS.\n\nProszę o kontakt w razie pytań." + Closing)
				: ("Zbliża się koniec ulgi ZUS ({ulga})", "{zwrot},\n\ninformuję, że okres preferencji ZUS ({ulga}) przysługujący {panuPani} kończy się {dataKoniecUlgi} (pozostało dni: {dniDoKoncaUlgi}). Po tym terminie obowiązywać będą wyższe składki ZUS.\n\nProszę o kontakt w razie pytań." + Closing),
		};
	}
}
