using EasyTax.App.Localization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

public sealed class DialogService(ILocalizer localizer) : IDialogService
{
	private XamlRoot? _xamlRoot;

	public void Attach(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

	public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
		=> ConfirmAsync(title, [new DialogText(message)], confirmText, cancelText);

	public async Task<bool> ConfirmAsync(string title, IReadOnlyList<DialogText> message, string confirmText, string cancelText)
	{
		var paragraphs = new StackPanel { Spacing = 12 };
		foreach (var paragraph in message)
		{
			paragraphs.Children.Add(new TextBlock
			{
				Text = paragraph.Text,
				TextWrapping = TextWrapping.Wrap,
				FontWeight = paragraph.IsBold ? FontWeights.Bold : FontWeights.Normal,
			});
		}

		var dialog = new ContentDialog
		{
			Title = title,
			Content = paragraphs,
			PrimaryButtonText = confirmText,
			CloseButtonText = cancelText,
			DefaultButton = ContentDialogButton.Close,
		};

		return await ShowAsync(dialog) == ContentDialogResult.Primary;
	}

	public async Task<UnsavedChangesChoice> AskUnsavedChangesAsync()
	{
		var dialog = new ContentDialog
		{
			Title = localizer["Dialog_Unsaved_Title"],
			Content = new TextBlock { Text = localizer["Dialog_Unsaved_Message"], TextWrapping = TextWrapping.Wrap },
			PrimaryButtonText = localizer["Dialog_Save"],
			SecondaryButtonText = localizer["Dialog_Discard"],
			CloseButtonText = localizer["Dialog_Cancel"],
			DefaultButton = ContentDialogButton.Primary,
		};

		return await ShowAsync(dialog) switch
		{
			ContentDialogResult.Primary => UnsavedChangesChoice.Save,
			ContentDialogResult.Secondary => UnsavedChangesChoice.Discard,
			_ => UnsavedChangesChoice.Cancel,
		};
	}

	public Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
	{
		dialog.XamlRoot = _xamlRoot ?? throw new InvalidOperationException("DialogService is not attached to a window.");
		return dialog.ShowAsync().AsTask();
	}
}
