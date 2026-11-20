using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Alerts;

public sealed partial class AlertsPage : Page
{
	public AlertsPage()
	{
		ViewModel = App.GetService<AlertsViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	public AlertsViewModel ViewModel { get; }

	private void OnOpenClick(object sender, RoutedEventArgs e)
	{
		if (((FrameworkElement)sender).DataContext is AlertRow row)
			ViewModel.OpenClientCommand.Execute(row);
	}

	private void OnToggleHandledClick(object sender, RoutedEventArgs e)
	{
		if (((FrameworkElement)sender).DataContext is AlertRow row)
			ViewModel.ToggleHandledCommand.Execute(row);
	}
}
