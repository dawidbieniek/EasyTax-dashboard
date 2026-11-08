using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace EasyTax.App.Infrastructure;

/// <summary>Shown instead of the main window when the app cannot start (for example an unreadable database). Built in code because it must not depend on anything that failed.</summary>
internal sealed class StartupErrorWindow : Window
{
	public StartupErrorWindow(string title, string message, string details, string closeText)
	{
		Title = title;
		AppWindow.Resize(new SizeInt32(560, 320));

		var close = new Button { Content = closeText, HorizontalAlignment = HorizontalAlignment.Right };
		close.Click += (_, _) => Close();

		var panel = new StackPanel
		{
			Padding = new Thickness(24),
			Spacing = 12,
			Children =
			{
				new TextBlock { Text = title, Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"] },
				new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
				new TextBlock { Text = details, TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true, Opacity = 0.7 },
				close,
			},
		};

		// A bare Window has no background of its own.
		Content = new Grid
		{
			Background = (Brush)Application.Current.Resources["ApplicationPageBackgroundThemeBrush"],
			Children = { panel },
		};

		WindowStack.Push(this);
		Closed += (_, _) => Application.Current.Exit();
	}
}
