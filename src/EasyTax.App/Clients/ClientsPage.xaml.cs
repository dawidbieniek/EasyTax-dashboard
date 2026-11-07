using EasyTax.App.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
	}

	public ClientsViewModel ViewModel { get; }

	private async void OnAddClientClick(object sender, RoutedEventArgs e)
	{
		var addViewModel = App.GetService<AddClientViewModel>();
		await addViewModel.InitializeAsync();

		var result = await App.GetService<IDialogService>().ShowAsync(new AddClientDialog(addViewModel));
		if (result == ContentDialogResult.Primary)
			await ViewModel.RefreshAsync();
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
