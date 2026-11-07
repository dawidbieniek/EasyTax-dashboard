using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Clients;

public sealed partial class ClientDetailsPage : Page
{
	public ClientDetailsPage()
	{
		ViewModel = App.GetService<ClientDetailsViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	public ClientDetailsViewModel ViewModel { get; }

	private void OnTabChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		if (sender.SelectedItem?.Tag is string tag && Enum.TryParse<ClientTab>(tag, out var tab))
			ViewModel.SelectedTab = tab;
	}
}
