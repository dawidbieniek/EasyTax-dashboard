using EasyTax.App.Localization;
using EasyTax.App.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace EasyTax.App.Security;

/// <summary>
/// The key saved on this computer is missing or does not open the database (new computer, new Windows profile). Asks for the
/// recovery key and checks it against the database before anything is kept.
/// </summary>
internal sealed class KeyRecoveryWindow : KeyWindow
{
	private readonly ILocalizer _localizer;
	private readonly Func<string, Task<bool>> _verify;
	private readonly TextBox _input = new();
	private readonly TextBlock _error = new() { TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"] };

	public KeyRecoveryWindow(ILocalizer localizer, AppTheme theme, Func<string, Task<bool>> verify)
		: base(localizer["Key_Recovery_Title"], theme)
	{
		_localizer = localizer;
		_verify = verify;

		_input.PlaceholderText = "XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX-XXXX";
		_input.FontFamily = new FontFamily("Consolas");
		_input.FontSize = 18;

		var next = new Button { Content = localizer["Key_Recovery_Continue"], Style = (Style)Application.Current.Resources["AccentButtonStyle"] };
		next.Click += async (_, _) => await TryContinueAsync();

		var quit = new Button { Content = localizer["Key_Recovery_Quit"] };
		quit.Click += (_, _) => Close();

		Panel.Children.Add(Heading(localizer["Key_Recovery_Title"]));
		Panel.Children.Add(Paragraph(localizer["Key_Recovery_Message"]));
		Panel.Children.Add(_input);
		Panel.Children.Add(_error);
		Panel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { next, quit } });
	}

	/// <summary>The recovery key that opened the database, normalized. Set when the window finished successfully.</summary>
	public string? Key { get; private set; }

	private async Task TryContinueAsync()
	{
		if (!DatabaseKeyService.TryNormalize(_input.Text, out var key))
		{
			_error.Text = _localizer["Key_Recovery_Invalid"];
			return;
		}

		if (!await _verify(key))
		{
			_error.Text = _localizer["Key_Recovery_Wrong"];
			return;
		}

		Key = key;
		Finish();
	}
}
