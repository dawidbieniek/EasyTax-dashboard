using EasyTax.App.Infrastructure;
using EasyTax.App.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace EasyTax.App.Security;

/// <summary>
/// Base of the windows shown before the main window exists (key setup, key recovery). Built in code so they do not depend on
/// anything that needs the database. <see cref="ShowAsync"/> completes with true when the user finished, false when the window was closed.
/// </summary>
internal abstract class KeyWindow : Window
{
	private readonly TaskCompletionSource<bool> _completion = new();

	protected KeyWindow(string title, AppTheme theme)
	{
		Title = title;
		AppWindow.Resize(new SizeInt32(680, 620));

		Panel = new StackPanel { Padding = new Thickness(28), Spacing = 14 };

		Content = new ScrollViewer
		{
			Content = Panel,
			Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
			RequestedTheme = ThemeService.ToElementTheme(theme),
		};

		Closed += (_, _) => _completion.TrySetResult(false);
	}

	/// <summary>The page content; derived windows add their controls here.</summary>
	protected StackPanel Panel { get; }

	public Task<bool> ShowAsync()
	{
		WindowStack.Push(this);
		Activate();
		return _completion.Task;
	}

	/// <summary>Ends the dialog successfully and closes the window.</summary>
	protected void Finish()
	{
		_completion.TrySetResult(true);
		Close();
	}

	protected static TextBlock Paragraph(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

	protected static TextBlock Heading(string text) => new() { Text = text, Style = (Style)Application.Current.Resources["TitleTextBlockStyle"], TextWrapping = TextWrapping.Wrap };
}
