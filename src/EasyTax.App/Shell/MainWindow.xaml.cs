using EasyTax.App.Clients;
using EasyTax.App.Dashboard;
using EasyTax.App.Settings;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace EasyTax.App.Shell;

/// <summary>Application shell: title bar, navigation rail, app bar and the frame that hosts pages. Contains no business logic.</summary>
public sealed partial class MainWindow : Window
{
	private const string DashboardTag = "Dashboard";
	private const string ClientsTag = "Clients";
	private const int MinimumWindowWidth = 1280;

	private readonly INavigationService _navigation;
	private ViewModelBase? _currentViewModel;

	public MainWindow(INavigationService navigation, IThemeService themeService, IDialogService dialogService, IShellWindow shellWindow)
	{
		_navigation = navigation;

		InitializeComponent();

		shellWindow.Attach(this);

		Title = "EasyTax";
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(AppTitleBar);
		AppWindow.Resize(new SizeInt32(1400, 900));


		themeService.Attach(this);

		// Dialogs need the XamlRoot of the content, which exists once the content has loaded.
		((FrameworkElement)Content).Loaded += (_, _) =>
		{
			dialogService.Attach(Content.XamlRoot);

			// The presenter works in pixels, the constant is in device-independent pixels.
			if (AppWindow.Presenter is OverlappedPresenter presenter)
				presenter.PreferredMinimumWidth = (int)(MinimumWindowWidth * Content.XamlRoot.RasterizationScale);
		};

		_navigation.Initialize(ContentFrame);
		_navigation.Navigated += OnNavigated;
		_navigation.NavigateTo<DashboardViewModel>();
	}

	private async void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		if (!await _navigation.CanLeaveAsync())
		{
			// The rail already moved its highlight; put it back on the page that stays.
			if (_currentViewModel is not null)
				SyncSelection(_currentViewModel);

			return;
		}

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
		_currentViewModel = viewModel;
		AppBarControl.SetMode(viewModel.AppBarMode);
		SyncSelection(viewModel);
	}

	private void SyncSelection(ViewModelBase viewModel)
	{
		if (viewModel is SettingsViewModel)
		{
			Navigation.SelectedItem = Navigation.SettingsItem;
			return;
		}

		// The client details page belongs to the clients section of the rail.
		var tag = viewModel is ClientsViewModel or ClientDetailsViewModel ? ClientsTag : DashboardTag;
		Navigation.SelectedItem = Navigation.MenuItems
			.OfType<NavigationViewItem>()
			.First(item => (string)item.Tag == tag);
	}
}
