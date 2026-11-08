using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

/// <summary>Shows modal dialogs over the main window, so view models never touch UI objects.</summary>
public interface IDialogService
{
	/// <summary>Gives the service the root of the main window's content. Called once by the shell.</summary>
	void Attach(XamlRoot xamlRoot);

	/// <summary>Yes/no question. Returns true when the user chose the confirm button.</summary>
	Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText);

	/// <summary>Yes/no question whose message is several paragraphs, each optionally bold.</summary>
	Task<bool> ConfirmAsync(string title, IReadOnlyList<DialogText> message, string confirmText, string cancelText);

	/// <summary>"You have unsaved changes": save, discard or stay.</summary>
	Task<UnsavedChangesChoice> AskUnsavedChangesAsync();

	/// <summary>Shows long read-only text (for example the licences) in a scrollable dialog with one close button.</summary>
	Task ShowTextAsync(string title, string text, string closeText);

	/// <summary>Shows a custom dialog (for example the add-client form).</summary>
	Task<ContentDialogResult> ShowAsync(ContentDialog dialog);
}
