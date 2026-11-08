using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace EasyTax.App.Shell;

/// <summary>Shared top bar: client picker, year, month strip and accounted toggle. Event glue only; the logic is in <see cref="AppBarViewModel"/>.</summary>
public sealed partial class AppBar : UserControl
{
	public AppBar()
	{
		ViewModel = App.GetService<AppBarViewModel>();
		InitializeComponent();
	}

	public AppBarViewModel ViewModel { get; }

	public void SetMode(AppBarMode mode)
	{
		Visibility = mode == AppBarMode.Hidden ? Visibility.Collapsed : Visibility.Visible;
		MonthControls.Visibility = mode == AppBarMode.Full ? Visibility.Visible : Visibility.Collapsed;
	}

	private void OnClientBoxGotFocus(object sender, RoutedEventArgs e)
	{
		// Start a fresh search and show the whole list as soon as the box is focused, so it works like a drop-down with search.
		var box = (AutoSuggestBox)sender;
		box.Text = string.Empty;
		box.ItemsSource = ViewModel.FindClients(null);
		box.IsSuggestionListOpen = true;
	}

	private void OnClientBoxLostFocus(object sender, RoutedEventArgs e)
	{
		// Throw away half-typed text and show the selected client again.
		var box = (AutoSuggestBox)sender;
		box.Text = ViewModel.SelectedClientName;
	}

	private void OnClientTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
			return;

		sender.ItemsSource = ViewModel.FindClients(sender.Text);
	}

	private async void OnClientSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
	{
		if (args.SelectedItem is ClientSuggestion suggestion)
			await ViewModel.SelectClientAsync(suggestion.Id);
	}

	private async void OnClientQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
	{
		var suggestion = args.ChosenSuggestion as ClientSuggestion ?? ViewModel.FindClients(args.QueryText).FirstOrDefault();
		if (suggestion is not null)
			await ViewModel.SelectClientAsync(suggestion.Id);
	}

	private async void OnYearSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.FirstOrDefault() is int year)
			await ViewModel.SelectYearAsync(year);
	}

	private async void OnMonthClick(object sender, RoutedEventArgs e)
	{
		if (sender is Button { Tag: int month })
			await ViewModel.SelectMonthAsync(month);
	}

	private async void OnAccountedClick(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleButton toggle)
			await ViewModel.SetAccountedAsync(toggle.IsChecked == true);
	}
}
