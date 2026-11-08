using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Settings;

public sealed partial class SettingsPage : Page
{
	public SettingsPage()
	{
		ViewModel = App.GetService<SettingsViewModel>();
		DataContext = ViewModel;
		InitializeComponent();
	}

	public SettingsViewModel ViewModel { get; }

	private void OnRemoveYearClick(object sender, RoutedEventArgs e)
	{
		if (sender is Button { Tag: int year })
			ViewModel.RemoveYearCommand.Execute(year);
	}
}
