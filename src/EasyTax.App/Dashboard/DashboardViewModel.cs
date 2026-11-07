using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Clients;
using EasyTax.App.Localization;
using EasyTax.App.Shell;

namespace EasyTax.App.Dashboard;

public sealed partial class DashboardViewModel : ViewModelBase
{
	private readonly IClientContext _context;
	private readonly INavigationService _navigation;
	private readonly ILocalizer _localizer;

	public DashboardViewModel(IClientContext context, INavigationService navigation, ILocalizer localizer)
	{
		_context = context;
		_navigation = navigation;
		_localizer = localizer;

		_context.Changed += OnContextChanged;
		Refresh();
	}

	public override AppBarMode AppBarMode => AppBarMode.Full;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasNoClients))]
	public partial bool HasClients { get; set; }

	/// <summary>Placeholder until the dashboard cards exist: shows what is currently selected.</summary>
	[ObservableProperty]
	public partial string Selection { get; set; } = string.Empty;

	public bool HasNoClients => !HasClients;

	public override void OnNavigatedFrom() => _context.Changed -= OnContextChanged;

	[RelayCommand]
	private void GoToClients() => _navigation.NavigateTo<ClientsViewModel>();

	private void OnContextChanged(object? sender, EventArgs e) => Refresh();

	private void Refresh()
	{
		HasClients = _context.SelectedClient is not null;

		Selection = _context.SelectedClient is { } client
			? string.Format(CultureInfo.CurrentCulture, _localizer["Dashboard_Selection"], client.FullName, _context.SelectedMonth.ToString("MMMM yyyy", CultureInfo.CurrentCulture))
			: string.Empty;
	}
}
