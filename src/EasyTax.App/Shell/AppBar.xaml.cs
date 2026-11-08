using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.System;

namespace EasyTax.App.Shell;

/// <summary>Shared top bar: client picker, year, month strip and accounted toggle. Event glue only; the logic is in <see cref="AppBarViewModel"/>.</summary>
public sealed partial class AppBar : UserControl
{
	public AppBar()
	{
		ViewModel = App.GetService<AppBarViewModel>();
		InitializeComponent();

		Shortcuts.Add(ClientBox, Shortcuts.Slash, FocusClientBox);
		Shortcuts.Add(ClientBox, VirtualKey.F, FocusClientBox, VirtualKeyModifiers.Control);
		Shortcuts.Add(PreviousClientButton, VirtualKey.Left, () => Run(ViewModel.SelectPreviousClientCommand), VirtualKeyModifiers.Menu);
		Shortcuts.Add(NextClientButton, VirtualKey.Right, () => Run(ViewModel.SelectNextClientCommand), VirtualKeyModifiers.Menu);

		Shortcuts.Add(YearBox, VirtualKey.Left, () => RunInFullMode(() => ViewModel.ShiftMonthAsync(-1)));
		Shortcuts.Add(YearBox, VirtualKey.Right, () => RunInFullMode(() => ViewModel.ShiftMonthAsync(1)));
		Shortcuts.Add(YearBox, Shortcuts.Comma, () => RunInFullMode(() => ViewModel.ShiftMonthAsync(-1)));
		Shortcuts.Add(YearBox, Shortcuts.Period, () => RunInFullMode(() => ViewModel.ShiftMonthAsync(1)));
		Shortcuts.Add(YearBox, Shortcuts.Comma, () => RunInFullMode(() => ViewModel.ShiftYearAsync(-1)), VirtualKeyModifiers.Shift);
		Shortcuts.Add(YearBox, Shortcuts.Period, () => RunInFullMode(() => ViewModel.ShiftYearAsync(1)), VirtualKeyModifiers.Shift);
		Shortcuts.Add(AccountedToggle, VirtualKey.A, () => RunInFullMode(ViewModel.ToggleAccountedAsync));
	}

	public AppBarViewModel ViewModel { get; }

	public void SetMode(AppBarMode mode)
	{
		Visibility = mode == AppBarMode.Hidden ? Visibility.Collapsed : Visibility.Visible;
		MonthControls.Visibility = mode == AppBarMode.Full ? Visibility.Visible : Visibility.Collapsed;
	}

	private void FocusClientBox()
	{
		if (Visibility == Visibility.Visible && ClientBox.IsEnabled)
			Shortcuts.FocusLater(ClientBox);
	}

	private void Run(System.Windows.Input.ICommand command)
	{
		if (Visibility == Visibility.Visible && command.CanExecute(null))
			command.Execute(null);
	}

	// Month, year and accounted controls exist only in full mode.
	private async void RunInFullMode(Func<Task> action)
	{
		if (Visibility == Visibility.Visible && MonthControls.Visibility == Visibility.Visible)
			await action();
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
