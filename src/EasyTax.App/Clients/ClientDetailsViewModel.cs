using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Localization;
using EasyTax.App.Photos;
using EasyTax.App.Shell;
using EasyTax.Core.Domain.Banks;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.App.Clients;

/// <summary>
/// The selected client's page: a read-only view that switches to an editable form. While editing, the app bar's client
/// picker is locked and leaving the page asks what to do with unsaved changes.
/// </summary>
public sealed partial class ClientDetailsViewModel : ViewModelBase
{
	private readonly IClientRepository _clients;
	private readonly IBankRepository _banks;
	private readonly IClientPhotoRepository _photos;
	private readonly IPhotoService _photoService;
	private readonly IClientContext _context;
	private readonly IDialogService _dialogs;
	private readonly INavigationService _navigation;
	private readonly ILocalizer _localizer;

	private Client? _client;

	public ClientDetailsViewModel(
		IClientRepository clients,
		IBankRepository banks,
		IClientPhotoRepository photos,
		IPhotoService photoService,
		IClientContext context,
		IDialogService dialogs,
		INavigationService navigation,
		ILocalizer localizer)
	{
		_clients = clients;
		_banks = banks;
		_photos = photos;
		_photoService = photoService;
		_context = context;
		_dialogs = dialogs;
		_navigation = navigation;
		_localizer = localizer;

		Form = new ClientForm(localizer);
		GenderOptions = localizer.Options<Gender>();
		TaxTypeOptions = localizer.Options<TaxType>();
		PaymentFrequencyOptions = localizer.Options<PaymentFrequency>();
		ReliefTypeOptions = localizer.Options<ReliefType>();


		Form.PropertyChanged += OnFormPropertyChanged;
		_context.Changed += OnContextChanged;
	}

	public override AppBarMode AppBarMode => AppBarMode.ClientOnly;

	public ClientForm Form { get; }

	public IReadOnlyList<EnumOption<Gender>> GenderOptions { get; }

	public IReadOnlyList<EnumOption<TaxType>> TaxTypeOptions { get; }

	public IReadOnlyList<EnumOption<PaymentFrequency>> PaymentFrequencyOptions { get; }

	public IReadOnlyList<EnumOption<ReliefType>> ReliefTypeOptions { get; }


	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SelectedBank))]
	public partial IReadOnlyList<Bank> Banks { get; set; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasNoClient))]
	public partial bool HasClient { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsViewing))]
	public partial bool IsEditing { get; set; }

	[ObservableProperty]
	public partial ClientDisplay? Display { get; set; }

	/// <summary>Accounted status of the month selected in the app bar.</summary>
	[ObservableProperty]
	public partial string MonthStatus { get; set; } = string.Empty;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasPhoto))]
	public partial byte[]? PhotoBytes { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsPersonalTab), nameof(IsTaxTab), nameof(IsSubscriptionTab))]
	public partial ClientTab SelectedTab { get; set; } = ClientTab.Personal;

	// Selected items of the combo boxes, kept in sync with the form (binding SelectedItem is reliable, SelectedValue is not).
	public EnumOption<Gender>? SelectedGender
	{
		get => GenderOptions.FirstOrDefault(option => option.Value == Form.Gender);
		set => Form.Gender = value?.Value ?? Form.Gender;
	}

	public EnumOption<TaxType>? SelectedTaxType
	{
		get => TaxTypeOptions.FirstOrDefault(option => option.Value == Form.TaxType);
		set => Form.TaxType = value?.Value ?? Form.TaxType;
	}

	public EnumOption<PaymentFrequency>? SelectedPitPaymentFrequency
	{
		get => PaymentFrequencyOptions.FirstOrDefault(option => option.Value == Form.PitPaymentFrequency);
		set => Form.PitPaymentFrequency = value?.Value ?? Form.PitPaymentFrequency;
	}

	public EnumOption<PaymentFrequency>? SelectedVatPaymentFrequency
	{
		get => PaymentFrequencyOptions.FirstOrDefault(option => option.Value == Form.VatPaymentFrequency);
		set => Form.VatPaymentFrequency = value?.Value ?? Form.VatPaymentFrequency;
	}

	public EnumOption<ReliefType>? SelectedReliefType
	{
		get => ReliefTypeOptions.FirstOrDefault(option => option.Value == Form.ReliefType);
		set => Form.ReliefType = value?.Value ?? Form.ReliefType;
	}

	public Bank? SelectedBank
	{
		get => Banks.FirstOrDefault(bank => bank.Id == Form.BankId);
		set => Form.BankId = value?.Id ?? Form.BankId;
	}

	public bool HasNoClient => !HasClient;

	public bool IsViewing => !IsEditing;

	public bool HasPhoto => PhotoBytes is not null;

	public bool IsPersonalTab => SelectedTab == ClientTab.Personal;

	public bool IsTaxTab => SelectedTab == ClientTab.TaxAndRelief;

	public bool IsSubscriptionTab => SelectedTab == ClientTab.Subscription;

	public override async Task OnNavigatedToAsync(object? parameter)
	{
		Banks = await _banks.GetAllAsync();
		await LoadAsync();
	}

	public override void OnNavigatedFrom()
	{
		_context.Changed -= OnContextChanged;
		_context.IsSelectionLocked = false;
	}

	public override async Task<bool> ConfirmLeaveAsync()
	{
		if (!IsEditing)
			return true;

		if (!Form.HasChanges)
		{
			EndEditing();
			return true;
		}

		switch (await _dialogs.AskUnsavedChangesAsync())
		{
			case UnsavedChangesChoice.Save:
				return await TrySaveAsync();
			case UnsavedChangesChoice.Discard:
				Discard();
				return true;
			default:
				return false;
		}
	}

	[RelayCommand]
	private void Edit()
	{
		if (_client is null)
			return;

		IsEditing = true;
		_context.IsSelectionLocked = true;
	}

	[RelayCommand]
	private Task SaveAsync() => TrySaveAsync();

	[RelayCommand]
	private async Task CancelAsync()
	{
		var discard = !Form.HasChanges || await _dialogs.ConfirmAsync(
			_localizer["Details_Discard_Title"],
			_localizer["Details_Discard_Message"],
			_localizer["Details_Discard_Confirm"],
			_localizer["Dialog_Cancel"]);

		if (discard)
			Discard();
	}

	[RelayCommand]
	private async Task DeleteAsync()
	{
		if (_client is null)
			return;

		if (!await ClientDeleteDialog.ConfirmAsync(_dialogs, _localizer, _client.FullName))
			return;

		EndEditing();
		await _clients.DeleteAsync(_client.Id);
		await _context.ReloadAsync();

		_navigation.NavigateTo<ClientsViewModel>();
	}

	[RelayCommand]
	private async Task ChangePhotoAsync()
	{
		if (_client is null)
			return;

		var photo = await _photoService.PickAsync();
		if (photo is null)
			return;

		await _photos.SaveAsync(_client.Id, photo.Bytes, photo.ContentType);
		await _context.ReloadAsync();
	}

	[RelayCommand]
	private async Task RemovePhotoAsync()
	{
		if (_client is null)
			return;

		await _photos.RemoveAsync(_client.Id);
		await _context.ReloadAsync();
	}

	/// <summary>Validates and saves the form. False when it is invalid, so the caller stays in edit mode.</summary>
	private async Task<bool> TrySaveAsync()
	{
		if (_client is null || !Form.Validate())
			return false;

		Form.ApplyTo(_client);
		await _clients.UpdateAsync(_client);
		await _context.ReloadAsync();

		await LoadAsync();
		EndEditing();

		return true;
	}

	private void Discard()
	{
		if (_client is not null)
			Form.Load(_client);

		EndEditing();
	}

	private void EndEditing()
	{
		IsEditing = false;
		_context.IsSelectionLocked = false;
	}

	/// <summary>Loads the selected client from the database into the display and the form.</summary>
	private async Task LoadAsync()
	{
		var selected = _context.SelectedClient;
		_client = selected is null ? null : await _clients.GetAsync(selected.Id);

		HasClient = _client is not null;
		RefreshFromContext();

		if (_client is null)
		{
			Display = null;
			return;
		}

		Display = ClientDisplay.From(_client, _localizer);
		Form.Load(_client);
	}

	private void RefreshFromContext()
	{
		PhotoBytes = _context.SelectedClientPhoto;

		MonthStatus = _context.SelectedClient is { } client
			? _localizer.Label(client.GetMonthState(_context.SelectedMonth, _context.YearRecords))
			: string.Empty;
	}

	private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		var affected = e.PropertyName switch
		{
			nameof(ClientForm.Gender) => nameof(SelectedGender),
			nameof(ClientForm.TaxType) => nameof(SelectedTaxType),
			nameof(ClientForm.PitPaymentFrequency) => nameof(SelectedPitPaymentFrequency),
			nameof(ClientForm.VatPaymentFrequency) => nameof(SelectedVatPaymentFrequency),
			nameof(ClientForm.ReliefType) => nameof(SelectedReliefType),
			nameof(ClientForm.BankId) => nameof(SelectedBank),
			_ => null,
		};

		if (affected is not null)
		{
			OnPropertyChanged(affected);
			return;
		}

		// The form was loaded: every selection may have changed.
		if (string.IsNullOrEmpty(e.PropertyName))
		{
			OnPropertyChanged(nameof(SelectedGender));
			OnPropertyChanged(nameof(SelectedTaxType));
			OnPropertyChanged(nameof(SelectedPitPaymentFrequency));
			OnPropertyChanged(nameof(SelectedVatPaymentFrequency));
			OnPropertyChanged(nameof(SelectedReliefType));
			OnPropertyChanged(nameof(SelectedBank));
		}
	}

	private async void OnContextChanged(object? sender, EventArgs e)
	{
		// Same client (photo or month changed): only refresh what the context owns. Another client: load it, unless unsaved edits are open.
		if (_context.SelectedClient?.Id == _client?.Id)
		{
			RefreshFromContext();
			return;
		}

		if (!IsEditing)
			await LoadAsync();
	}
}
