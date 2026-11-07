using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace EasyTax.App.Photos;

/// <summary>Turns the stored photo bytes into an image for <c>PersonPicture.ProfilePicture</c>. No bytes means no image, so initials show instead.</summary>
public sealed partial class PhotoImageConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not byte[] { Length: > 0 } bytes)
			return null;

		// The stream must outlive this method: the image keeps decoding from it afterwards, so it is not disposed here.
		var stream = new InMemoryRandomAccessStream();
		var writer = stream.AsStreamForWrite();
		writer.Write(bytes);
		writer.Flush();
		stream.Seek(0);

		var image = new BitmapImage();
		image.SetSource(stream);

		return image;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
		=> throw new NotSupportedException();
}
