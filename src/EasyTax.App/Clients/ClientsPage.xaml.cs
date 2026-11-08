using EasyTax.App.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using WinUI.TableView;

namespace EasyTax.App.Clients;

public sealed partial class ClientsPage : Page
{
	// The row the context menu was opened for.
	private ClientRow? _contextRow;

	public ClientsPage()
	{
		ViewModel = App.GetService<ClientsViewModel>();
		DataContext = ViewModel;
		InitializeComponent();

		Shortcuts.Add(SearchBox, Shortcuts.Slash, () => Shortcuts.FocusLater(SearchBox));
		Shortcuts.Add(SearchBox, VirtualKey.F, () => Shortcuts.FocusLater(SearchBox), VirtualKeyModifiers.Control);
		Shortcuts.Add(AddButton, VirtualKey.N, OnAddShortcut);
		Shortcuts.Add(AddButton, VirtualKey.N, OnAddShortcut, VirtualKeyModifiers.Control);
		Shortcuts.Add(ClientsGrid, VirtualKey.Delete, OnDeleteShortcut);
		Shortcuts.Add(ClientsGrid, VirtualKey.Enter, OnOpenShortcut);
	}

	public ClientsViewModel ViewModel { get; }

	private void OnAddShortcut() => OnAddClientClick(this, new RoutedEventArgs());

	private async void OnDeleteShortcut()
	{
		if (ClientsGrid.SelectedItem is ClientRow row)
			await ViewModel.DeleteCommand.ExecuteAsync(row);
	}

	// Enter opens the selected client, but only while the grid itself has focus (Enter on a button must keep working).
	private async void OnOpenShortcut()
	{
		if (FocusManager.GetFocusedElement(XamlRoot) is not DependencyObject focused || FindRow(focused) is null)
			return;

		if (ClientsGrid.SelectedItem is ClientRow row)
			await ViewModel.OpenCommand.ExecuteAsync(row);
	}

	private async void OnAddClientClick(object sender, RoutedEventArgs e)
	{
		var addViewModel = App.GetService<AddClientViewModel>();
		await addViewModel.InitializeAsync();

		var result = await App.GetService<IDialogService>().ShowAsync(new AddClientDialog(addViewModel));
		if (result != ContentDialogResult.Primary)
			return;

		await ViewModel.RefreshAsync();
		ViewModel.NotifyClientAdded(addViewModel.DisplayName);
	}

	private async void OnGridTapped(object sender, TappedRoutedEventArgs e)
	{
		// A tap on a row opens that client; taps on headers and empty space are ignored.
		if (FindRow(e.OriginalSource as DependencyObject) is { Content: ClientRow row })
			await ViewModel.OpenCommand.ExecuteAsync(row);
	}

	private static TableViewRow? FindRow(DependencyObject? element)
	{
		while (element is not null and not TableViewRow)
			element = VisualTreeHelper.GetParent(element);

		return element as TableViewRow;
	}

	private void OnRowContextFlyoutOpening(object sender, TableViewRowContextFlyoutEventArgs e)
	{
		_contextRow = e.Item as ClientRow;
		SuspendItem.Text = _contextRow?.SuspendLabel ?? string.Empty;
	}

	private async void OnOpenClick(object sender, RoutedEventArgs e)
		=> await ViewModel.OpenCommand.ExecuteAsync(_contextRow);

	private async void OnSuspendClick(object sender, RoutedEventArgs e)
		=> await ViewModel.ToggleSuspendCommand.ExecuteAsync(_contextRow);

	private async void OnDeleteClick(object sender, RoutedEventArgs e)
		=> await ViewModel.DeleteCommand.ExecuteAsync(_contextRow);
}
