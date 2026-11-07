using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Clients;

public sealed partial class ClientsPage : Page
{
	public ClientsPage()
	{
		ViewModel = App.GetService<ClientsViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	public ClientsViewModel ViewModel { get; }
}
