using EasyTax.App.Alerts;
using EasyTax.App.Clients;
using EasyTax.App.Dashboard;
using EasyTax.App.Settings;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;
using Windows.System;

namespace EasyTax.App.Shell;

/// <summary>Application shell: title bar, navigation rail, app bar and the frame that hosts pages. Contains no business logic.</summary>
public sealed partial class MainWindow : Window
{
	private const string DashboardTag = "Dashboard";
	private const string ClientsTag = "Clients";
	private const string AlertsTag = "Alerts";
	private const int MinimumWindowWidth = 1280;

	private readonly INavigationService _navigation;
	private readonly IToastService _toasts;
	private readonly IAlertService _alerts;
	private ViewModelBase? _currentViewModel;

	public MainWindow(INavigationService navigation, IThemeService themeService, IDialogService dialogService, IShellWindow shellWindow, IToastService toastService, IAlertService alertService)
	{
		_navigation = navigation;
		_toasts = toastService;
		_alerts = alertService;

		InitializeComponent();

		shellWindow.Attach(this);

		Shortcuts.Add(DashboardItem, VirtualKey.Number1, () => _ = ShowAsync(typeof(DashboardViewModel)));
		Shortcuts.Add(ClientsItem, VirtualKey.Number2, () => _ = ShowAsync(typeof(ClientsViewModel)));
		Shortcuts.Add(AlertsItem, VirtualKey.Number4, () => _ = ShowAsync(typeof(AlertsViewModel)));
		Shortcuts.Add(RootGrid, VirtualKey.Escape, () => Shortcuts.Blur(RootGrid.XamlRoot, ContentFrame), whileTyping: true);
		Shortcuts.BlurOnClickAway(RootGrid, ContentFrame);

		Title = "EasyTax";
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(AppTitleBar);
		AppWindow.Resize(new SizeInt32(1400, 900));


		themeService.Attach(this);

		// Dialogs need the XamlRoot of the content, which exists once the content has loaded.
		((FrameworkElement)Content).Loaded += (_, _) =>
		{
			dialogService.Attach(Content.XamlRoot);

			// The badge follows the alert list; the first evaluation happens now that the database is ready.
			_alerts.Changed += (_, _) => UpdateAlertsBadge();
			_ = _alerts.RefreshAsync();

			if (Navigation.SettingsItem is NavigationViewItem settingsItem)
				Shortcuts.Add(settingsItem, VirtualKey.Number3, () => _ = ShowAsync(typeof(SettingsViewModel)));

			// The presenter works in pixels, the constant is in device-independent pixels.
			if (AppWindow.Presenter is OverlappedPresenter presenter)
				presenter.PreferredMinimumWidth = (int)(MinimumWindowWidth * Content.XamlRoot.RasterizationScale);
		};

		_navigation.Initialize(ContentFrame);
		_navigation.Navigated += OnNavigated;
		_navigation.NavigateTo<DashboardViewModel>();
	}

	public System.Collections.ObjectModel.ObservableCollection<ToastItem> Toasts => _toasts.Toasts;

	private void OnToastClosed(InfoBar sender, InfoBarClosedEventArgs args)
	{
		if (sender.DataContext is ToastItem toast)
			_toasts.Dismiss(toast);
	}

	// Keyboard navigation: the rail did not move its highlight, so there is nothing to put back when leaving is refused.
	private async Task ShowAsync(Type viewModelType)
	{
		if (_currentViewModel?.GetType() == viewModelType)
			return;

		if (!await _navigation.CanLeaveAsync())
			return;

		_navigation.NavigateTo(viewModelType);
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
			case AlertsTag:
				_navigation.NavigateTo<AlertsViewModel>();
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
		var tag = viewModel switch
		{
			ClientsViewModel or ClientDetailsViewModel => ClientsTag,
			AlertsViewModel => AlertsTag,
			_ => DashboardTag,
		};
		Navigation.SelectedItem = Navigation.MenuItems
			.OfType<NavigationViewItem>()
			.First(item => (string)item.Tag == tag);
	}

	private void UpdateAlertsBadge()
	{
		var count = _alerts.ActiveCount;
		AlertsBadge.Value = count;
		AlertsBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
	}
}
