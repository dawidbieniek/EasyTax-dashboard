using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Localization;
using EasyTax.App.Photos;
using EasyTax.Core.Domain.Banks;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.App.Clients;

public sealed partial class AddClientViewModel : ObservableObject
{
	private readonly IClientRepository _clients;
	private readonly IBankRepository _banks;
	private readonly IClientPhotoRepository _photos;
	private readonly IPhotoService _photoService;

	private ProcessedPhoto? _photo;

	public AddClientViewModel(
		IClientRepository clients,
		IBankRepository banks,
		IClientPhotoRepository photos,
		IPhotoService photoService,
		ILocalizer localizer)
	{
		_clients = clients;
		_banks = banks;
		_photos = photos;
		_photoService = photoService;

		Form = new ClientForm(localizer);
		Form.PropertyChanged += OnFormPropertyChanged;
	}

	public ClientForm Form { get; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SelectedBank))]
	public partial IReadOnlyList<Bank> Banks { get; set; } = [];

	/// <summary>Selected item of the bank combo box, kept in sync with the form.</summary>
	public Bank? SelectedBank
	{
		get => Banks.FirstOrDefault(bank => bank.Id == Form.BankId);
		set => Form.BankId = value?.Id;
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasPhoto))]
	public partial byte[]? PhotoBytes { get; set; }

	public bool HasPhoto => PhotoBytes is not null;

	/// <summary>Name typed so far; the avatar shows its initials while the user types.</summary>
	public string DisplayName => $"{Form.FirstName} {Form.LastName}".Trim();

	public async Task InitializeAsync()
	{
		Banks = await _banks.GetAllAsync();
		Form.Reset();
	}

	/// <summary>Checks the form and shows the errors. True when the client can be created.</summary>
	public bool Validate() => Form.Validate();

	/// <summary>Saves the new client (and its photo, if one was chosen). Call <see cref="Validate"/> first.</summary>
	public async Task<Client> CreateAsync()
	{
		var client = Form.ToNewClient();
		await _clients.AddAsync(client);

		if (_photo is not null)
			await _photos.SaveAsync(client.Id, _photo.Bytes, _photo.ContentType);

		return client;
	}

	[RelayCommand]
	private async Task PickPhotoAsync()
	{
		var photo = await _photoService.PickAsync();
		if (photo is null)
			return;

		_photo = photo;
		PhotoBytes = photo.Bytes;
	}

	[RelayCommand]
	private void RemovePhoto()
	{
		_photo = null;
		PhotoBytes = null;
	}

	private void OnFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(ClientForm.FirstName) or nameof(ClientForm.LastName))
			OnPropertyChanged(nameof(DisplayName));

		if (e.PropertyName is nameof(ClientForm.BankId))
			OnPropertyChanged(nameof(SelectedBank));
	}
}
