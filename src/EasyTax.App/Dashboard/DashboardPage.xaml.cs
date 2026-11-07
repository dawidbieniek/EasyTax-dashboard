using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Dashboard;

public sealed partial class DashboardPage : Page
{
	public DashboardPage()
	{
		ViewModel = App.GetService<DashboardViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	public DashboardViewModel ViewModel { get; }
}
