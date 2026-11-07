using Microsoft.Windows.ApplicationModel.Resources;

namespace EasyTax.App.Localization;

public sealed class Localizer : ILocalizer
{
	private readonly ResourceLoader _loader = new();

	public string this[string key] => _loader.GetString(key);
}
