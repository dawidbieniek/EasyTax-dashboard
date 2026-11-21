using EasyTax.App.Localization;
using EasyTax.Core.Domain.Changelog;
using Microsoft.UI.Xaml;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

public sealed class DialogService(ILocalizer localizer) : IDialogService
{
	private readonly SemaphoreSlim _one = new(1, 1);
	private XamlRoot? _xamlRoot;

	public void Attach(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

	public Task<bool> ConfirmAsync(string title, string message, string confirmText, string cancelText)
		=> ConfirmAsync(title, [new DialogText(message)], confirmText, cancelText);

	public async Task<bool> ConfirmAsync(string title, IReadOnlyList<DialogText> message, string confirmText, string cancelText, bool destructive = false)
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

		if (destructive)
			dialog.PrimaryButtonStyle = (Style)Application.Current.Resources["DestructiveButtonStyle"];

		return await ShowAsync(dialog) == ContentDialogResult.Primary;
	}

	public Task ShowTextAsync(string title, string text, string closeText)
	{
		// A read-only TextBox showed only the first line of a long text; a selectable TextBlock in a ScrollViewer does not.
		var content = new ScrollViewer
		{
			Content = new TextBlock
			{
				Text = text,
				IsTextSelectionEnabled = true,
				TextWrapping = TextWrapping.Wrap,
				FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
				FontSize = 12,
				Margin = new Thickness(0, 0, 16, 0),
			},
			MinWidth = 640,
			MaxHeight = 420,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
		};

		var dialog = new ContentDialog
		{
			Title = title,
			Content = content,
			CloseButtonText = closeText,
			DefaultButton = ContentDialogButton.Close,
		};

		// The standard dialog is narrower than this content, which clipped the right edge of long lines.
		dialog.Resources["ContentDialogMaxWidth"] = 760d;

		return ShowAsync(dialog);
	}

	public Task ShowChangelogAsync(string title, IReadOnlyList<ChangelogRelease> releases, string closeText)
	{
		var secondary = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
		var list = new StackPanel { Spacing = 20, Margin = new Thickness(0, 0, 16, 0) };

		foreach (var release in releases)
		{
			var chapter = new StackPanel { Spacing = 8 };
			var heading = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
			heading.Children.Add(new TextBlock
			{
				Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, localizer["Changelog_Version"], release.Version.ToString(3)),
				Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
			});
			if (release.Date is { } date)
			{
				heading.Children.Add(new TextBlock
				{
					Text = date.ToString("d", System.Globalization.CultureInfo.CurrentCulture),
					Foreground = secondary,
					VerticalAlignment = VerticalAlignment.Bottom,
				});
			}

			chapter.Children.Add(heading);

			foreach (var section in release.Sections)
			{
				chapter.Children.Add(new TextBlock { Text = localizer[$"Changelog_{section.Kind}"], FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
				foreach (var item in section.Items)
				{
					// A hanging bullet: the text wraps under itself, not under the dot.
					var row = new Grid { ColumnSpacing = 8, Margin = new Thickness(8, 0, 0, 0) };
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
					row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
					row.Children.Add(new TextBlock { Text = "\u2022" });
					var text = new TextBlock { Text = item, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true };
					Grid.SetColumn(text, 1);
					row.Children.Add(text);
					chapter.Children.Add(row);
				}
			}

			list.Children.Add(chapter);
		}

		var dialog = new ContentDialog
		{
			Title = title,
			Content = new ScrollViewer { Content = list, MinWidth = 560, MaxHeight = 460, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
			CloseButtonText = closeText,
			DefaultButton = ContentDialogButton.Close,
		};
		dialog.Resources["ContentDialogMaxWidth"] = 720d;

		return ShowAsync(dialog);
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

	public async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
	{
		var xamlRoot = _xamlRoot ?? throw new InvalidOperationException("DialogService is not attached to a window.");
		dialog.XamlRoot = xamlRoot;

		// Dialogs live in their own popup layer and do not inherit the theme chosen in the app, only the system one.
		if (xamlRoot.Content is FrameworkElement root)
			dialog.RequestedTheme = root.RequestedTheme;

		// WinUI allows one dialog at a time and throws when a second is shown; a second request waits for the first to close.
		await _one.WaitAsync();
		try
		{
			return await dialog.ShowAsync();
		}
		finally
		{
			_one.Release();
		}
	}
}
