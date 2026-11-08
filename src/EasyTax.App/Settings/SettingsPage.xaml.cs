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

	private void OnTabChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		// Fires while the page is still loading, before the panels below exist; they start out in the right state.
		if (AboutPanel is null)
			return;

		var tab = sender.SelectedItem?.Tag as string;

		GeneralPanel.Visibility = tab == "General" ? Visibility.Visible : Visibility.Collapsed;
		DataPanel.Visibility = tab == "Data" ? Visibility.Visible : Visibility.Collapsed;
		AboutPanel.Visibility = tab == "About" ? Visibility.Visible : Visibility.Collapsed;
	}

	private void OnRemoveYearClick(object sender, RoutedEventArgs e)
	{
		if (sender is Button { Tag: int year })
			ViewModel.RemoveYearCommand.Execute(year);
	}
}
