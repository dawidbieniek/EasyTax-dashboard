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
	ChipItem Status,
	string ContactLine,
	IReadOnlyList<ChipItem> Chips)
{
	public static ClientDisplay From(Client client, ILocalizer localizer)
	{
		string YesNo(bool value) => localizer[value ? "Common_Yes" : "Common_No"];

		var reliefEnd = ClientFormatting.Date(client.ReliefEndDate);

		var chips = new List<ChipItem>
		{
			new(localizer.Label(client.TaxType), ChipItem.Blue),
			new($"{localizer["Chip_Pit"]}: {localizer.Label(client.PitPaymentFrequency).ToLower(ClientFormatting.Culture)}", ChipItem.Indigo),
		};

		if (client.IsVatPayer)
			chips.Add(new($"{localizer["Chip_Vat"]}: {localizer.Label(client.VatPaymentFrequency).ToLower(ClientFormatting.Culture)}", ChipItem.Teal));

		if (client.UsesCashRegister)
			chips.Add(new(localizer["Chip_CashRegister"], ChipItem.Pink));

		chips.Add(new(
			client.ReliefEndDate is null
				? localizer.Label(client.ReliefType)
				: $"{localizer.Label(client.ReliefType)} · {localizer["Chip_Until"]} {reliefEnd}",
			ChipItem.Purple));

		if (!string.IsNullOrWhiteSpace(client.PlanName))
			chips.Add(new($"{localizer["Chip_Plan"]}: {client.PlanName}", ChipItem.BlueGrey));

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
			StatusText(client, localizer),
			string.Join(" · ", contact),
			chips);
	}

	// "Active", or "Suspended since <date>" (the date is shown in local time).
	private static ChipItem StatusText(Client client, ILocalizer localizer)
	{
		if (client.SuspendedAt is not { } since)
			return new ChipItem(localizer["Details_Status_Active"], ChipItem.Green);

		var date = ClientFormatting.Date(DateOnly.FromDateTime(since.ToLocalTime()));

		return new ChipItem(string.Format(System.Globalization.CultureInfo.CurrentCulture, localizer["Details_Status_SuspendedSince"], date), ChipItem.Amber);
	}
}
