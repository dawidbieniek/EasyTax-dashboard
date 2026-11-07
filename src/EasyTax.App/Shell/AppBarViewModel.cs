using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Localization;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;

namespace EasyTax.App.Shell;

/// <summary>State of the shared app bar. A thin projection of <see cref="IClientContext"/>; all selection logic lives there.</summary>
public sealed partial class AppBarViewModel : ObservableObject
{
	private readonly IClientContext _context;
	private readonly ILocalizer _localizer;

	public AppBarViewModel(IClientContext context, ILocalizer localizer)
	{
		_context = context;
		_localizer = localizer;

		_context.Changed += (_, _) => Refresh();
		Refresh();
	}

	public ObservableCollection<MonthItem> Months { get; } = [];

	[ObservableProperty]
	public partial bool HasClients { get; set; }

	[ObservableProperty]
	public partial string SelectedClientName { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string ClientPlaceholder { get; set; } = string.Empty;

	[ObservableProperty]
	public partial IReadOnlyList<int> Years { get; set; } = [];

	[ObservableProperty]
	public partial int SelectedYear { get; set; }

	[ObservableProperty]
	public partial bool IsAccounted { get; set; }

	[ObservableProperty]
	public partial bool CanAccount { get; set; }

	[ObservableProperty]
	public partial string AccountedLabel { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool CanSelectPrevious { get; set; }

	[ObservableProperty]
	public partial bool CanSelectNext { get; set; }

	/// <summary>Clients whose name contains the text (case-insensitive); all of them when the text is empty.</summary>
	public IReadOnlyList<Client> FindClients(string? text)
		=> string.IsNullOrWhiteSpace(text)
			? _context.Clients
			: [.. _context.Clients.Where(c => c.FullName.Contains(text.Trim(), StringComparison.CurrentCultureIgnoreCase))];

	public Task SelectClientAsync(Guid clientId) => _context.SelectClientAsync(clientId);

	public Task SelectYearAsync(int year) => _context.SelectYearAsync(year);

	public Task SelectMonthAsync(int month) => _context.SelectMonthAsync(month);

	public Task SetAccountedAsync(bool isAccounted) => _context.SetAccountedAsync(isAccounted);

	[RelayCommand]
	private Task SelectPreviousClientAsync() => _context.SelectAdjacentClientAsync(-1);

	[RelayCommand]
	private Task SelectNextClientAsync() => _context.SelectAdjacentClientAsync(1);

	private void Refresh()
	{
		var client = _context.SelectedClient;

		HasClients = _context.Clients.Count > 0;
		SelectedClientName = client?.FullName ?? string.Empty;
		ClientPlaceholder = _localizer[HasClients ? "AppBar_ClientPlaceholder" : "AppBar_NoClients"];
		CanSelectPrevious = _context.CanSelectAdjacentClient(-1);
		CanSelectNext = _context.CanSelectAdjacentClient(1);

		// Replacing the list would clear the combo box selection, so only do it when the years really changed.
		if (!Years.SequenceEqual(_context.Years))
			Years = _context.Years;
		SelectedYear = _context.Year;

		CanAccount = client?.IsMonthSelectable(_context.SelectedMonth) ?? false;
		IsAccounted = _context.CurrentRecord?.IsAccounted ?? false;
		AccountedLabel = _localizer[IsAccounted ? "AppBar_Accounted" : "AppBar_NotAccounted"];

		Months.Clear();
		for (var month = 1; month <= 12; month++)
		{
			var state = client?.GetMonthState(new DateOnly(_context.Year, month, 1), _context.YearRecords) ?? MonthState.BeforeJoin;
			Months.Add(new MonthItem(month, _localizer[$"Month_{month}"], state, month == _context.Month));
		}
	}
}
