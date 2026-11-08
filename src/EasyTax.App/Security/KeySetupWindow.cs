using System.Diagnostics;
using EasyTax.App.Localization;
using EasyTax.App.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace EasyTax.App.Security;

/// <summary>
/// First start: shows the recovery key and does not let the user continue until they have copied it somewhere and typed back
/// its first and last group. The key is only stored (and the database only created) after this window finished.
/// </summary>
internal sealed class KeySetupWindow : KeyWindow
{
	private readonly ILocalizer _localizer;
	private readonly string _formattedKey;
	private readonly string _expectedCheck;
	private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Opacity = 0.8 };
	private readonly TextBox _check = new();
	private readonly Button _continue = new() { IsEnabled = false };

	public KeySetupWindow(string key, ILocalizer localizer, AppTheme theme)
		: base(localizer["Key_Setup_Title"], theme)
	{
		_localizer = localizer;
		_formattedKey = DatabaseKeyService.Format(key);

		var groups = _formattedKey.Split('-');
		_expectedCheck = groups[0] + groups[^1];

		var keyText = new TextBlock
		{
			Text = _formattedKey,
			FontFamily = new FontFamily("Consolas"),
			FontSize = 22,
			IsTextSelectionEnabled = true,
			TextWrapping = TextWrapping.Wrap,
			HorizontalAlignment = HorizontalAlignment.Center,
		};

		var keyBox = new Border
		{
			Child = keyText,
			Padding = new Thickness(16),
			CornerRadius = new CornerRadius(8),
			Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
		};

		var copy = new Button { Content = localizer["Key_Setup_Copy"] };
		copy.Click += (_, _) => Copy();
		var save = new Button { Content = localizer["Key_Setup_Save"] };
		save.Click += async (_, _) => await SaveAsync();
		var print = new Button { Content = localizer["Key_Setup_Print"] };
		print.Click += async (_, _) => await PrintAsync();

		_check.PlaceholderText = localizer["Key_Setup_CheckPlaceholder"];
		_check.TextChanged += (_, _) => _continue.IsEnabled = Matches();
		_continue.Content = localizer["Key_Setup_Continue"];
		_continue.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
		_continue.Click += (_, _) => Finish();

		Panel.Children.Add(Heading(localizer["Key_Setup_Title"]));
		Panel.Children.Add(Paragraph(localizer["Key_Setup_Message"]));
		Panel.Children.Add(keyBox);
		Panel.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { copy, save, print } });
		Panel.Children.Add(_status);
		Panel.Children.Add(Paragraph(localizer["Key_Setup_CheckPrompt"]));
		Panel.Children.Add(_check);
		Panel.Children.Add(_continue);
	}

	private bool Matches() => Normalize(_check.Text) == _expectedCheck;

	private static string Normalize(string text) => text.Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

	private string FileText() => string.Join(Environment.NewLine, _localizer["Key_Setup_FileHeader"], string.Empty, _formattedKey, string.Empty, _localizer["Key_Setup_FileNote"]);

	private void Copy()
	{
		var package = new DataPackage();
		package.SetText(_formattedKey);
		Clipboard.SetContent(package);
		_status.Text = _localizer["Key_Setup_Copied"];
	}

	private async Task SaveAsync()
	{
		var picker = new FileSavePicker { SuggestedFileName = "EasyTax-klucz-odzyskiwania" };
		picker.FileTypeChoices.Add("Tekst", [".txt"]);
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

		var file = await picker.PickSaveFileAsync();
		if (file is null)
			return;

		await FileIO.WriteTextAsync(file, FileText());
		_status.Text = _localizer["Key_Setup_Saved"];
	}

	private async Task PrintAsync()
	{
		try
		{
			// A temporary text file, printed by whatever program is set up for text files. Deleted afterwards.
			var path = Path.Combine(Path.GetTempPath(), "EasyTax-klucz-odzyskiwania.txt");
			await File.WriteAllTextAsync(path, FileText());

			using var process = Process.Start(new ProcessStartInfo(path) { Verb = "print", UseShellExecute = true, CreateNoWindow = true });
			if (process is not null)
				await process.WaitForExitAsync();

			await Task.Delay(TimeSpan.FromSeconds(5));
			File.Delete(path);
			_status.Text = _localizer["Key_Setup_Printed"];
		}
		catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or InvalidOperationException)
		{
			_status.Text = _localizer["Key_Setup_PrintFailed"];
		}
	}
}
