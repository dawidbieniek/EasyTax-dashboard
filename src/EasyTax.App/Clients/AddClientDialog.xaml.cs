using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Clients;

public sealed partial class AddClientDialog : ContentDialog
{
	public AddClientDialog(AddClientViewModel viewModel)
	{
		ViewModel = viewModel;
		InitializeComponent();
	}

	public AddClientViewModel ViewModel { get; }

	private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		// Keep the dialog open while saving, and keep it open (with the errors shown) when the form is invalid.
		var deferral = args.GetDeferral();
		try
		{
			if (!ViewModel.Validate())
			{
				args.Cancel = true;
				return;
			}

			await ViewModel.CreateAsync();
		}
		finally
		{
			deferral.Complete();
		}
	}
}
