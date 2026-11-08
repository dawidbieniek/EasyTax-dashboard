using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace EasyTax.App.Dashboard;

/// <summary>Turns a plain colour into a brush, for colours that are computed (such as the theme-aware limit colours) rather than taken from XAML resources.</summary>
public sealed partial class ColorBrushConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
		=> new SolidColorBrush(value is Color color ? color : default);

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException();
}
