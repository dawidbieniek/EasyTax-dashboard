using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Backups;
using EasyTax.App.Infrastructure;
using EasyTax.App.Localization;
using EasyTax.App.Shell;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;
using Microsoft.Extensions.Logging;

namespace EasyTax.App.Clients;

public sealed partial class ClientsViewModel(
	IClientRepository clients,
	IMonthRecordRepository months,
	IClientContext context,
	INavigationService navigation,
	IDialogService dialogs,
	ILocalizer localizer,
	IToastService toasts,
	ClientDeletionService deletion,
	ILogger<ClientsViewModel> logger) : ViewModelBase
{
	private IReadOnlyList<ClientRow> _allRows = [];

	public ObservableCollection<ClientRow> Rows { get; } = [];

	[ObservableProperty]
	public partial string SearchText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Summary { get; set; } = string.Empty;

	/// <summary>Why the list could not be loaded; empty when it loaded fine. The page shows an error bar with a retry button.</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasLoadError))]
	public partial string LoadError { get; set; } = string.Empty;

	public bool HasLoadError => LoadError.Length > 0;

	public override Task OnNavigatedToAsync(object? parameter) => LoadAsync();

	/// <summary>Reloads the list from the database. Also called after a client was added.</summary>
	public async Task LoadAsync()
	{
		try
		{
			await LoadRowsAsync();
			LoadError = string.Empty;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Could not load the client list");
			LoadError = UserFacingErrors.Describe(ex, localizer);
		}
	}

	/// <summary>Shows the "client added" toast; the add dialog itself belongs to the page.</summary>
	public void NotifyClientAdded(string name) => toasts.Success(string.Format(CultureInfo.CurrentCulture, localizer["Toast_ClientAdded"], name));

	[RelayCommand]
	private Task RetryAsync() => LoadAsync();

	private async Task LoadRowsAsync()
	{
		var allClients = await clients.GetAllAsync();

		// The "accounted" column follows the month selected in the app bar.
		var selectedMonth = context.SelectedMonth;
		var monthRecords = await months.GetForMonthAsync(selectedMonth);
		var recordsByClient = monthRecords.ToLookup(r => r.ClientId);

		_allRows = [.. allClients.Select(client => ToRow(client, selectedMonth, recordsByClient[client.Id], context.ClientPhotos.GetValueOrDefault(client.Id)))];
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

		// Suspending records the moment; reactivating clears it.
		client.SuspendedAt = client.IsSuspended ? null : DateTime.UtcNow;
		await clients.UpdateAsync(client);

		await context.ReloadAsync(dropSuspendedSelection: true);
		await LoadAsync();

		var key = client.IsSuspended ? "Toast_ClientSuspended" : "Toast_ClientActivated";
		toasts.Success(string.Format(CultureInfo.CurrentCulture, localizer[key], client.FullName));
	}

	[RelayCommand]
	private async Task DeleteAsync(ClientRow? row)
	{
		if (row is null)
			return;

		if (!await ClientDeleteDialog.ConfirmAsync(dialogs, localizer, row.FullName, row.SuspendedAt))
			return;

		await deletion.DeleteAsync(row.Id);

		await context.ReloadAsync();
		await LoadAsync();

		toasts.Success(string.Format(CultureInfo.CurrentCulture, localizer["Toast_ClientDeleted"], row.FullName));
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
			client.SuspendedAt,
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
