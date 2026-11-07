using EasyTax.App.Localization;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.App.Clients;

/// <summary>A client's data formatted for reading: every value is ready-to-show text.</summary>
public sealed record ClientDisplay(
	string FullName,
	string FirstName,
	string LastName,
	string Gender,
	string Nip,
	string PhoneNumber,
	string Email,
	string JoinDate,
	string Bank,
	string PlanName,
	string AuthorizationFee,
	string SubscriptionFee,
	string EmploymentContract,
	string CashMethod,
	string TaxType,
	string PitPaymentFrequency,
	string IsVatPayer,
	string VatPaymentFrequency,
	string UsesCashRegister,
	string FirstCashRegisterDate,
	string Relief,
	string ReliefSinceDate,
	string ReliefEndDate,
	string Status,
	string ContactLine,
	IReadOnlyList<string> Chips)
{
	public static ClientDisplay From(Client client, ILocalizer localizer)
	{
		string YesNo(bool value) => localizer[value ? "Common_Yes" : "Common_No"];

		var reliefEnd = ClientFormatting.Date(client.ReliefEndDate);

		var chips = new List<string>
		{
			localizer.Label(client.TaxType),
			$"{localizer["Chip_Pit"]}: {localizer.Label(client.PitPaymentFrequency).ToLower(ClientFormatting.Culture)}",
		};

		if (client.IsVatPayer)
			chips.Add($"{localizer["Chip_Vat"]}: {localizer.Label(client.VatPaymentFrequency).ToLower(ClientFormatting.Culture)}");

		if (client.UsesCashRegister)
			chips.Add(localizer["Chip_CashRegister"]);

		chips.Add(client.ReliefEndDate is null
			? localizer.Label(client.ReliefType)
			: $"{localizer.Label(client.ReliefType)} · {localizer["Chip_Until"]} {reliefEnd}");

		if (!string.IsNullOrWhiteSpace(client.PlanName))
			chips.Add($"{localizer["Chip_Plan"]}: {client.PlanName}");

		var contact = new List<string> { $"NIP {ClientFormatting.Nip(client.Nip)}" };
		if (!string.IsNullOrWhiteSpace(client.Email))
			contact.Add(client.Email);

		if (!string.IsNullOrWhiteSpace(client.PhoneNumber))
			contact.Add(client.PhoneNumber);

		return new ClientDisplay(
			client.FullName,
			client.FirstName,
			client.LastName,
			localizer.Label(client.Gender),
			ClientFormatting.Nip(client.Nip),
			ClientFormatting.OrDash(client.PhoneNumber),
			ClientFormatting.OrDash(client.Email),
			ClientFormatting.Date(client.JoinDate),
			client.Bank.Name,
			ClientFormatting.OrDash(client.PlanName),
			ClientFormatting.Money(client.AuthorizationFee),
			ClientFormatting.Money(client.SubscriptionFee),
			YesNo(client.EmploymentContract),
			YesNo(client.CashMethod),
			localizer.Label(client.TaxType),
			localizer.Label(client.PitPaymentFrequency),
			YesNo(client.IsVatPayer),
			client.IsVatPayer ? localizer.Label(client.VatPaymentFrequency) : "—",
			YesNo(client.UsesCashRegister),
			client.UsesCashRegister ? ClientFormatting.Date(client.FirstCashRegisterDate) : "—",
			localizer.Label(client.ReliefType),
			client.ReliefType == ReliefType.ZusPlus ? ClientFormatting.Date(client.ReliefSinceDate) : "—",
			reliefEnd,
			localizer[client.IsSuspended ? "Details_Status_Suspended" : "Details_Status_Active"],
			string.Join(" · ", contact),
			chips);
	}
}
