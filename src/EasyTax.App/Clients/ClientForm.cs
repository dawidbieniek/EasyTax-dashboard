using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyTax.App.Localization;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.App.Clients;

/// <summary>
/// Editable copy of a client's data, shared by the add-client dialog and the details page.
/// Knows how to validate itself, whether it differs from what was loaded, and how to write itself into a <see cref="Client"/>.
/// </summary>
public sealed partial class ClientForm(ILocalizer localizer) : ObservableObject
{
	private static readonly HashSet<string> DerivedNames =
	[
		nameof(FirstNameError),
		nameof(LastNameError),
		nameof(NipError),
		nameof(PhoneNumberError),
		nameof(EmailError),
		nameof(JoinDateError),
		nameof(BankError),
		nameof(HasChanges),
		nameof(IsVatPaymentEnabled),
		nameof(IsFirstCashRegisterDateEnabled),
		nameof(IsReliefSinceEnabled),
		nameof(ReliefEndText),
	];

	private ClientFormValues? _original;
	private bool _loading;
	private bool _submitted;

	// Personal
	[ObservableProperty]
	public partial string FirstName { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string LastName { get; set; } = string.Empty;

	[ObservableProperty]
	public partial Gender Gender { get; set; } = Gender.Male;

	[ObservableProperty]
	public partial string Nip { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PhoneNumber { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Email { get; set; } = string.Empty;

	[ObservableProperty]
	public partial DateTimeOffset? JoinDate { get; set; } = DateTimeOffset.Now.Date;

	[ObservableProperty]
	public partial Guid? BankId { get; set; }

	// Subscription
	[ObservableProperty]
	public partial string PlanName { get; set; } = string.Empty;

	[ObservableProperty]
	public partial double AuthorizationFee { get; set; }

	[ObservableProperty]
	public partial double SubscriptionFee { get; set; }

	[ObservableProperty]
	public partial bool EmploymentContract { get; set; }

	[ObservableProperty]
	public partial bool CashMethod { get; set; }

	// Taxes
	[ObservableProperty]
	public partial TaxType TaxType { get; set; } = TaxType.Scale;

	[ObservableProperty]
	public partial PaymentFrequency PitPaymentFrequency { get; set; } = PaymentFrequency.Monthly;

	[ObservableProperty]
	public partial bool IsVatPayer { get; set; }

	[ObservableProperty]
	public partial PaymentFrequency VatPaymentFrequency { get; set; } = PaymentFrequency.Monthly;

	[ObservableProperty]
	public partial bool UsesCashRegister { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset? FirstCashRegisterDate { get; set; }

	// Relief
	[ObservableProperty]
	public partial ReliefType ReliefType { get; set; } = ReliefType.Start;

	[ObservableProperty]
	public partial DateTimeOffset? ReliefSinceDate { get; set; }

	// Validation messages (empty = fine)
	[ObservableProperty]
	public partial string FirstNameError { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string LastNameError { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string NipError { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PhoneNumberError { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string EmailError { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string JoinDateError { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string BankError { get; set; } = string.Empty;

	public bool HasChanges => _original is not null && ToValues() != _original;

	public bool IsVatPaymentEnabled => IsVatPayer;

	public bool IsFirstCashRegisterDateEnabled => UsesCashRegister;

	public bool IsReliefSinceEnabled => ReliefType == ReliefType.ZusPlus;

	/// <summary>When the relief entered in the form would end, calculated from the other fields as they are typed.</summary>
	public string ReliefEndText
	{
		get
		{
			var preview = new Client
			{
				FirstName = string.Empty,
				LastName = string.Empty,
				Nip = string.Empty,
				JoinDate = ToDateOnly(JoinDate) ?? DateOnly.FromDateTime(DateTime.Today),
				BankId = Guid.Empty,
				ReliefType = ReliefType,
				ReliefSinceDate = ToDateOnly(ReliefSinceDate),
			};

			return ClientFormatting.Date(preview.ReliefEndDate);
		}
	}

	/// <summary>Fills the form from a client and remembers those values as the baseline for <see cref="HasChanges"/>.</summary>
	public void Load(Client client)
	{
		_loading = true;

		FirstName = client.FirstName;
		LastName = client.LastName;
		Gender = client.Gender;
		Nip = ClientFormatting.Nip(client.Nip);
		PhoneNumber = client.PhoneNumber;
		Email = client.Email;
		JoinDate = ToOffset(client.JoinDate);
		BankId = client.BankId;
		PlanName = client.PlanName;
		AuthorizationFee = (double)client.AuthorizationFee;
		SubscriptionFee = (double)client.SubscriptionFee;
		EmploymentContract = client.EmploymentContract;
		CashMethod = client.CashMethod;
		TaxType = client.TaxType;
		PitPaymentFrequency = client.PitPaymentFrequency;
		IsVatPayer = client.IsVatPayer;
		VatPaymentFrequency = client.VatPaymentFrequency;
		UsesCashRegister = client.UsesCashRegister;
		FirstCashRegisterDate = ToOffset(client.FirstCashRegisterDate);
		ReliefType = client.ReliefType;
		ReliefSinceDate = ToOffset(client.ReliefSinceDate);

		_loading = false;
		_submitted = false;
		_original = ToValues();

		UpdateErrors();
		OnPropertyChanged(new PropertyChangedEventArgs(string.Empty));
	}

	/// <summary>Starts a blank form (add client): the baseline is the empty form, errors stay hidden until the first submit.</summary>
	public void Reset()
	{
		_submitted = false;
		_original = ToValues();
		UpdateErrors();
		OnPropertyChanged(new PropertyChangedEventArgs(string.Empty));
	}

	/// <summary>Checks everything, showing required-field errors too. Returns true when the form can be saved.</summary>
	public bool Validate()
	{
		_submitted = true;
		UpdateErrors();

		return new[] { FirstNameError, LastNameError, NipError, PhoneNumberError, EmailError, JoinDateError, BankError }
			.All(string.IsNullOrEmpty);
	}

	/// <summary>Writes the form into an existing client. Call <see cref="Validate"/> first.</summary>
	public void ApplyTo(Client client)
	{
		client.FirstName = FirstName.Trim();
		client.LastName = LastName.Trim();
		client.Gender = Gender;
		client.Nip = ClientValidation.NormalizeNip(Nip);
		client.PhoneNumber = PhoneNumber.Trim();
		client.Email = Email.Trim();
		client.JoinDate = ToDateOnly(JoinDate) ?? client.JoinDate;
		client.BankId = BankId ?? client.BankId;
		client.PlanName = PlanName.Trim();
		client.AuthorizationFee = Math.Round((decimal)AuthorizationFee, 2);
		client.SubscriptionFee = Math.Round((decimal)SubscriptionFee, 2);
		client.EmploymentContract = EmploymentContract;
		client.CashMethod = CashMethod;
		client.TaxType = TaxType;
		client.PitPaymentFrequency = PitPaymentFrequency;
		client.IsVatPayer = IsVatPayer;
		client.VatPaymentFrequency = VatPaymentFrequency;
		client.UsesCashRegister = UsesCashRegister;
		client.FirstCashRegisterDate = ToDateOnly(FirstCashRegisterDate);
		client.ReliefType = ReliefType;
		client.ReliefSinceDate = ToDateOnly(ReliefSinceDate);
	}

	/// <summary>Builds a new client from the form. Call <see cref="Validate"/> first.</summary>
	public Client ToNewClient()
	{
		var client = new Client
		{
			FirstName = FirstName.Trim(),
			LastName = LastName.Trim(),
			Nip = ClientValidation.NormalizeNip(Nip),
			JoinDate = ToDateOnly(JoinDate) ?? DateOnly.FromDateTime(DateTime.Today),
			BankId = BankId ?? Guid.Empty,
		};

		ApplyTo(client);
		return client;
	}

	protected override void OnPropertyChanged(PropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (_loading || e.PropertyName is null || DerivedNames.Contains(e.PropertyName))
			return;

		OnPropertyChanged(nameof(HasChanges));
		OnPropertyChanged(nameof(IsVatPaymentEnabled));
		OnPropertyChanged(nameof(IsFirstCashRegisterDateEnabled));
		OnPropertyChanged(nameof(IsReliefSinceEnabled));
		OnPropertyChanged(nameof(ReliefEndText));
		UpdateErrors();
	}

	/// <summary>Format errors show as soon as something invalid is typed; "required" errors only after the first submit.</summary>
	private void UpdateErrors()
	{
		var required = localizer["Form_Error_Required"];

		FirstNameError = _submitted && string.IsNullOrWhiteSpace(FirstName) ? required : string.Empty;
		LastNameError = _submitted && string.IsNullOrWhiteSpace(LastName) ? required : string.Empty;
		JoinDateError = _submitted && JoinDate is null ? required : string.Empty;
		BankError = _submitted && BankId is null ? required : string.Empty;

		NipError = string.IsNullOrWhiteSpace(Nip)
			? _submitted ? required : string.Empty
			: ClientValidation.IsValidNip(Nip) ? string.Empty : localizer["Form_Error_Nip"];
		PhoneNumberError = ClientValidation.IsValidPhone(PhoneNumber) ? string.Empty : localizer["Form_Error_Phone"];
		EmailError = ClientValidation.IsValidEmail(Email) ? string.Empty : localizer["Form_Error_Email"];
	}

	private ClientFormValues ToValues() => new(
		FirstName,
		LastName,
		Gender,
		Nip,
		PhoneNumber,
		Email,
		JoinDate,
		BankId,
		PlanName,
		AuthorizationFee,
		SubscriptionFee,
		EmploymentContract,
		CashMethod,
		TaxType,
		PitPaymentFrequency,
		IsVatPayer,
		VatPaymentFrequency,
		UsesCashRegister,
		FirstCashRegisterDate,
		ReliefType,
		ReliefSinceDate);

	private static DateOnly? ToDateOnly(DateTimeOffset? value)
		=> value is { } date ? DateOnly.FromDateTime(date.DateTime) : null;

	private static DateTimeOffset? ToOffset(DateOnly? value)
		=> value is { } date ? new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue)) : null;
}
