using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Localization;
using EasyTax.App.Shell;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;

namespace EasyTax.App.Clients;

public sealed partial class ClientsViewModel(
	IClientRepository clients,
	IMonthRecordRepository months,
	IClientPhotoRepository photos,
	IClientContext context,
	INavigationService navigation,
	IDialogService dialogs,
	ILocalizer localizer) : ViewModelBase
{
	private IReadOnlyList<ClientRow> _allRows = [];

	public ObservableCollection<ClientRow> Rows { get; } = [];

	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Summary { get; set; } = string.Empty;

	public override Task OnNavigatedToAsync(object? parameter) => LoadAsync();

	/// <summary>Reloads the list from the database. Also called after a client was added.</summary>
	public async Task LoadAsync()
	{
		var allClients = await clients.GetAllAsync();
		var photoByClient = await photos.GetAllAsync();

		// The "accounted" column follows the month selected in the app bar.
		var selectedMonth = context.SelectedMonth;
		var monthRecords = await months.GetForMonthAsync(selectedMonth);
		var recordsByClient = monthRecords.ToLookup(r => r.ClientId);

		_allRows = [.. allClients.Select(client => ToRow(client, selectedMonth, recordsByClient[client.Id], photoByClient.GetValueOrDefault(client.Id)))];
		Summary = string.Format(CultureInfo.CurrentCulture, localizer["Clients_Count"], _allRows.Count);

		ApplyFilter();
	}

	/// <summary>Called after a client was added or changed elsewhere: refreshes the shared context and this list.</summary>
	public async Task RefreshAsync()
	{
		await context.ReloadAsync();
		await LoadAsync();
	}

	partial void OnSearchTextChanged(string value) => ApplyFilter();

	[RelayCommand]
	private async Task OpenAsync(ClientRow? row)
	{
		if (row is null)
			return;

		await context.SelectClientAsync(row.Id);
		navigation.NavigateTo<ClientDetailsViewModel>();
	}

	[RelayCommand]
	private async Task ToggleSuspendAsync(ClientRow? row)
	{
		if (row is null)
			return;

		var client = await clients.GetAsync(row.Id);
		if (client is null)
			return;

		client.IsSuspended = !client.IsSuspended;
		await clients.UpdateAsync(client);

		await context.ReloadAsync(dropSuspendedSelection: true);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task DeleteAsync(ClientRow? row)
	{
		if (row is null)
			return;

		if (!await ClientDeleteDialog.ConfirmAsync(dialogs, localizer, row.FullName))
			return;

		await clients.DeleteAsync(row.Id);

		await context.ReloadAsync();
		await LoadAsync();
	}

	private ClientRow ToRow(Client client, DateOnly month, IEnumerable<MonthRecord> records, byte[]? photo)
	{
		var monthState = client.GetMonthState(month, records);

		return new ClientRow(
			client.Id,
			client.FullName,
			ClientFormatting.Nip(client.Nip),
			client.Bank.Name,
			localizer.Label(client.TaxType),
			localizer.Label(client.ReliefType),
			client.PhoneNumber,
			client.Email,
			monthState,
			localizer.Label(monthState),
			client.IsSuspended,
			localizer[client.IsSuspended ? "List_Unsuspend" : "List_Suspend"],
			photo);
	}

	private void ApplyFilter()
	{
		var text = SearchText.Trim();

		var matching = string.IsNullOrEmpty(text)
			? _allRows
			: [.. _allRows.Where(row => Matches(row, text))];

		Rows.Clear();
		foreach (var row in matching)
			Rows.Add(row);
	}

	private static bool Matches(ClientRow row, string text)
		=> new[] { row.FullName, row.Nip, row.BankName, row.Phone, row.Email }
			.Any(value => value.Contains(text, StringComparison.CurrentCultureIgnoreCase));
}
