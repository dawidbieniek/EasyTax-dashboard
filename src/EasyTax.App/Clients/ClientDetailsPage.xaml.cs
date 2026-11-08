using EasyTax.App.Shell;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

namespace EasyTax.App.Clients;

public sealed partial class ClientDetailsPage : Page
{
	public ClientDetailsPage()
	{
		ViewModel = App.GetService<ClientDetailsViewModel>();
		DataContext = ViewModel;
		InitializeComponent();

		Shortcuts.Add(EditButton, VirtualKey.E, () => RunWhen(ViewModel.IsViewing, ViewModel.EditCommand));
		Shortcuts.Add(DeleteButton, VirtualKey.Delete, () => RunWhen(ViewModel.IsViewing, ViewModel.DeleteCommand));
		Shortcuts.Add(SaveButton, VirtualKey.S, () => RunWhen(ViewModel.IsEditing, ViewModel.SaveCommand), VirtualKeyModifiers.Control);
		Shortcuts.Add(CancelButton, VirtualKey.Escape, () => RunWhen(ViewModel.IsEditing, ViewModel.CancelCommand), whileTyping: true);
	}

	public ClientDetailsViewModel ViewModel { get; }

	// The buttons of the other mode are hidden but their shortcuts would still fire, so check the mode first.
	private static void RunWhen(bool condition, System.Windows.Input.ICommand command)
	{
		if (condition && command.CanExecute(null))
			command.Execute(null);
	}

	private void OnTabChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
		if (sender.SelectedItem?.Tag is string tag && Enum.TryParse<ClientTab>(tag, out var tab))
			ViewModel.SelectedTab = tab;
	}
}
