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
}
