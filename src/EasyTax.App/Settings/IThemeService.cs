using Microsoft.UI.Xaml;

namespace EasyTax.App.Settings;

public interface IThemeService
{
	/// <summary>Applies the configured theme to the window and keeps it in sync with the settings.</summary>
	void Attach(Window window);
}
