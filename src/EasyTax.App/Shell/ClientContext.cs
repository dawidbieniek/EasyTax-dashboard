using EasyTax.App.Settings;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;
using EasyTax.Core.Domain.Years;

namespace EasyTax.App.Shell;

public sealed class ClientContext(
	IClientRepository clientRepository,
	IAccountingYearRepository yearRepository,
	IMonthRecordRepository monthRepository,
	ISettingsService settings) : IClientContext
{
	private IReadOnlyList<MonthRecord> _yearRecords = [];
	private int _version;

	public event EventHandler? Changed;

	public IReadOnlyList<Client> Clients { get; private set; } = [];

	public IReadOnlyList<int> Years { get; private set; } = [];

	public Client? SelectedClient { get; private set; }

	public int Year { get; private set; }

	public int Month { get; private set; }

	public DateOnly SelectedMonth => new(Year, Month, 1);

	public IReadOnlyList<MonthRecord> YearRecords => _yearRecords;

	public MonthRecord? CurrentRecord => _yearRecords.FirstOrDefault(r => r.Month == SelectedMonth);

	public async Task InitializeAsync()
	{
		await LoadListsAsync();

		var saved = settings.Current;
		var today = DateOnly.FromDateTime(DateTime.Today);

		var year = saved.LastYear is { } savedYear && Years.Contains(savedYear)
			? savedYear
			: NearestYear(today.Year);
		var month = saved.LastMonth is >= 1 and <= 12
			? saved.LastMonth.Value
			: year == today.Year ? today.Month : 1;
		var client = Clients.FirstOrDefault(c => c.Id == saved.LastClientId) ?? Clients.FirstOrDefault();

		await ApplyAsync(client, year, month);
	}

	public async Task ReloadAsync()
	{
		await LoadListsAsync();

		var client = Clients.FirstOrDefault(c => c.Id == SelectedClient?.Id) ?? Clients.FirstOrDefault();
		var year = Years.Contains(Year) ? Year : NearestYear(Year);

		await ApplyAsync(client, year, Month);
	}

	public async Task SelectClientAsync(Guid clientId)
	{
		var client = Clients.FirstOrDefault(c => c.Id == clientId);
		if (client is null || client.Id == SelectedClient?.Id)
			return;

		await ApplyAsync(client, Year, Month);
	}

	public Task SelectAdjacentClientAsync(int offset)
	{
		var index = IndexOfSelectedClient() + offset;
		if (!CanSelectAdjacentClient(offset))
			return Task.CompletedTask;

		return ApplyAsync(Clients[index], Year, Month);
	}

	public bool CanSelectAdjacentClient(int offset)
	{
		var index = IndexOfSelectedClient();
		if (index < 0)
			return false;

		var target = index + offset;
		return target >= 0 && target < Clients.Count;
	}

	public async Task SelectYearAsync(int year)
	{
		if (year == Year || !Years.Contains(year))
			return;

		await ApplyAsync(SelectedClient, year, Month);
	}

	public async Task SelectMonthAsync(int month)
	{
		if (month == Month || month is < 1 or > 12)
			return;

		if (SelectedClient is { } client && !client.IsMonthSelectable(new DateOnly(Year, month, 1)))
			return;

		await ApplyAsync(SelectedClient, Year, month);
	}

	public async Task UpdateCurrentRecordAsync(Action<MonthRecord> change)
	{
		if (SelectedClient is not { } client || !client.IsMonthSelectable(SelectedMonth))
			return;

		var record = CurrentRecord ?? new MonthRecord { ClientId = client.Id, Month = SelectedMonth };
		change(record);

		await monthRepository.SaveAsync(record);
		_yearRecords = await monthRepository.GetForYearAsync(client.Id, Year);

		Changed?.Invoke(this, EventArgs.Empty);
	}

	public Task SetAccountedAsync(bool isAccounted) => UpdateCurrentRecordAsync(record => record.IsAccounted = isAccounted);

	private async Task LoadListsAsync()
	{
		var allClients = await clientRepository.GetAllAsync();
		Clients = [.. allClients.Where(c => !c.IsSuspended)];
		Years = [.. (await yearRepository.GetAllAsync()).Select(y => y.Year)];
	}

	private async Task ApplyAsync(Client? client, int year, int month)
	{
		(year, month) = ClampToClient(client, year, month);

		SelectedClient = client;
		Year = year;
		Month = month;

		var version = ++_version;
		var records = client is null ? [] : await monthRepository.GetForYearAsync(client.Id, year);

		// A newer selection was made while loading: let that one publish its own result.
		if (version != _version)
			return;

		_yearRecords = records;

		settings.Update(s => s with { LastClientId = client?.Id, LastYear = year, LastMonth = month });
		Changed?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>Moves the selection to the client's join month when it would point at a month before they joined.</summary>
	private (int Year, int Month) ClampToClient(Client? client, int year, int month)
	{
		if (client is null || client.IsMonthSelectable(new DateOnly(year, month, 1)))
			return (year, month);

		var joinMonth = client.JoinMonth;
		return Years.Contains(joinMonth.Year) ? (joinMonth.Year, joinMonth.Month) : (year, month);
	}

	private int NearestYear(int around)
		=> Years.Count == 0 ? around : Years.OrderBy(y => Math.Abs(y - around)).First();

	private int IndexOfSelectedClient()
	{
		for (var i = 0; i < Clients.Count; i++)
		{
			if (Clients[i].Id == SelectedClient?.Id)
				return i;
		}

		return -1;
	}
}
