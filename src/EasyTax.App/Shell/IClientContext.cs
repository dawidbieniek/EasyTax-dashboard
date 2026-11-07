using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;

namespace EasyTax.App.Shell;

/// <summary>
/// The client, year and month the user is currently looking at. Shared by the app bar and every page that shows
/// per-client data, and remembered between runs.
/// </summary>
public interface IClientContext
{
	/// <summary>Clients that can be picked in the app bar (suspended ones excluded), ordered by name.</summary>
	IReadOnlyList<Client> Clients { get; }

	/// <summary>The user-managed years, ascending.</summary>
	IReadOnlyList<int> Years { get; }

	/// <summary>
	/// The selected client. Usually one of <see cref="Clients"/>, but a suspended client can be selected explicitly
	/// (opened from the clients list) and stays selected until another selection or a reload that drops it.
	/// </summary>
	Client? SelectedClient { get; }

	/// <summary>Picture of the selected client, or null when there is none.</summary>
	byte[]? SelectedClientPhoto { get; }

	int Year { get; }

	int Month { get; }

	/// <summary>First day of the selected month.</summary>
	DateOnly SelectedMonth { get; }

	/// <summary>Month records of the selected client in the selected year.</summary>
	IReadOnlyList<MonthRecord> YearRecords { get; }

	/// <summary>The record of the selected month, or null when nothing has been recorded yet.</summary>
	MonthRecord? CurrentRecord { get; }

	/// <summary>
	/// While true the client cannot be changed (a page is editing the current client and would lose its changes).
	/// The app bar disables its client controls.
	/// </summary>
	bool IsSelectionLocked { get; set; }

	/// <summary>Raised whenever the selection, the lock or the loaded data changed.</summary>
	event EventHandler? Changed;

	/// <summary>Loads the lists and restores the last selection. Call once at startup.</summary>
	Task InitializeAsync();

	/// <summary>
	/// Reloads clients, years, records and photo after they were edited elsewhere, keeping the selection when possible.
	/// </summary>
	/// <param name="dropSuspendedSelection">Move to the first available client when the selected one is now suspended.</param>
	Task ReloadAsync(bool dropSuspendedSelection = false);

	/// <summary>Selects any existing client, suspended or not.</summary>
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
