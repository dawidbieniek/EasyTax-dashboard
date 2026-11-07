using EasyTax.App.Clients;
using EasyTax.App.Dashboard;
using EasyTax.App.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace EasyTax.App.Shell;

/// <summary>Application shell: title bar, navigation rail and the frame that hosts pages. Contains no business logic.</summary>
public sealed partial class MainWindow : Window
{
	private const string DashboardTag = "Dashboard";
	private const string ClientsTag = "Clients";

	private readonly INavigationService _navigation;

	public MainWindow(INavigationService navigation, IThemeService themeService)
	{
		_navigation = navigation;

		InitializeComponent();

		Title = "EasyTax";
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(AppTitleBar);
		AppWindow.Resize(new SizeInt32(1400, 900));

		themeService.Attach(this);

		_navigation.Initialize(ContentFrame);
		_navigation.Navigated += OnNavigated;
		_navigation.NavigateTo<DashboardViewModel>();
	}

	private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		if (args.IsSettingsInvoked)
		{
			_navigation.NavigateTo<SettingsViewModel>();
			return;
		}

		switch (args.InvokedItemContainer?.Tag)
		{
			case DashboardTag:
				_navigation.NavigateTo<DashboardViewModel>();
				break;
			case ClientsTag:
				_navigation.NavigateTo<ClientsViewModel>();
				break;
		}
	}

	/// <summary>Keeps the highlighted rail item and the app bar in sync with the page that is shown.</summary>
	private void OnNavigated(object? sender, ViewModelBase viewModel)
	{
		AppBarControl.SetMode(viewModel.AppBarMode);

		var viewModelType = viewModel.GetType();
		if (viewModelType == typeof(SettingsViewModel))
		{
			Navigation.SelectedItem = Navigation.SettingsItem;
			return;
		}

		var tag = viewModelType == typeof(ClientsViewModel) ? ClientsTag : DashboardTag;
		Navigation.SelectedItem = Navigation.MenuItems
			.OfType<NavigationViewItem>()
			.First(item => (string)item.Tag == tag);
	}
}
