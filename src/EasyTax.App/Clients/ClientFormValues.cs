using EasyTax.Core.Domain.Clients;

namespace EasyTax.App.Clients;

/// <summary>Snapshot of everything the client form edits. Record equality tells whether anything changed.</summary>
public sealed record ClientFormValues(
	string FirstName,
	string LastName,
	Gender Gender,
	string Nip,
	string PhoneNumber,
	string Email,
	DateTimeOffset? JoinDate,
	Guid? BankId,
	string PlanName,
	double AuthorizationFee,
	double SubscriptionFee,
	bool EmploymentContract,
	bool CashMethod,
	TaxType TaxType,
	PaymentFrequency PitPaymentFrequency,
	bool IsVatPayer,
	PaymentFrequency VatPaymentFrequency,
	bool UsesCashRegister,
	DateTimeOffset? FirstCashRegisterDate,
	ReliefType ReliefType,
	DateTimeOffset? ReliefSinceDate);
