using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;

namespace EasyTax.App.Shell;

/// <summary>
/// The client, year and month the user is currently looking at. Shared by the app bar and every page that shows
/// per-client data, and remembered between runs.
/// </summary>
public interface IClientContext
{
	/// <summary>Clients that can be selected (suspended ones excluded), ordered by name.</summary>
	IReadOnlyList<Client> Clients { get; }

	/// <summary>The user-managed years, ascending.</summary>
	IReadOnlyList<int> Years { get; }

	Client? SelectedClient { get; }

	int Year { get; }

	int Month { get; }

	/// <summary>First day of the selected month.</summary>
	DateOnly SelectedMonth { get; }

	/// <summary>Month records of the selected client in the selected year.</summary>
	IReadOnlyList<MonthRecord> YearRecords { get; }

	/// <summary>The record of the selected month, or null when nothing has been recorded yet.</summary>
	MonthRecord? CurrentRecord { get; }

	/// <summary>Raised whenever the selection or the loaded data changed.</summary>
	event EventHandler? Changed;

	/// <summary>Loads the lists and restores the last selection. Call once at startup.</summary>
	Task InitializeAsync();

	/// <summary>Reloads clients, years and records after they were edited elsewhere, keeping the selection when possible.</summary>
	Task ReloadAsync();

	Task SelectClientAsync(Guid clientId);

	/// <summary>Selects the previous (-1) or next (+1) client in the list. Does nothing at the ends.</summary>
	Task SelectAdjacentClientAsync(int offset);

	bool CanSelectAdjacentClient(int offset);

	Task SelectYearAsync(int year);

	/// <summary>Selects a month (1–12) of the selected year. Months before the client joined are ignored.</summary>
	Task SelectMonthAsync(int month);

	/// <summary>Applies a change to the record of the selected month, creating it when needed, and saves it.</summary>
	Task UpdateCurrentRecordAsync(Action<MonthRecord> change);

	Task SetAccountedAsync(bool isAccounted);
}
