using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using EasyTax.App.Shell;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace EasyTax.App.Photos;

public sealed class PhotoService(IShellWindow shellWindow) : IPhotoService
{
	private const int PhotoSize = 256;
	private const string ContentType = "image/jpeg";

	public async Task<ProcessedPhoto?> PickAsync()
	{
		var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.PicturesLibrary };
		foreach (var extension in new[] { ".jpg", ".jpeg", ".png", ".bmp" })
			picker.FileTypeFilter.Add(extension);

		InitializeWithWindow.Initialize(picker, shellWindow.Handle);

		var file = await picker.PickSingleFileAsync();
		if (file is null)
			return null;

		try
		{
			return await ProcessAsync(file);
		}
		catch (Exception ex) when (ex is IOException or COMException or ArgumentException)
		{
			// Not a decodable picture: treat it like a cancelled pick.
			return null;
		}
	}

	/// <summary>Crops the picture to a centred square, shrinks it to at most 256 px and re-encodes it as JPEG.</summary>
	private static async Task<ProcessedPhoto> ProcessAsync(StorageFile file)
	{
		using var source = await file.OpenReadAsync();
		var decoder = await BitmapDecoder.CreateAsync(source);

		var side = Math.Min(decoder.PixelWidth, decoder.PixelHeight);
		var target = Math.Min(side, PhotoSize);

		// Scale so the shorter side becomes the target size, then crop the centred square.
		// The crop bounds are in the scaled image's coordinates, not the original's.
		var scale = (double)target / side;
		var scaledWidth = (uint)Math.Round(decoder.PixelWidth * scale);
		var scaledHeight = (uint)Math.Round(decoder.PixelHeight * scale);

		var transform = new BitmapTransform
		{
			ScaledWidth = scaledWidth,
			ScaledHeight = scaledHeight,
			Bounds = new BitmapBounds
			{
				X = (scaledWidth - target) / 2,
				Y = (scaledHeight - target) / 2,
				Width = target,
				Height = target,
			},
			InterpolationMode = BitmapInterpolationMode.Fant,
		};

		var pixels = await decoder.GetPixelDataAsync(
			BitmapPixelFormat.Bgra8,
			BitmapAlphaMode.Ignore,
			transform,
			ExifOrientationMode.IgnoreExifOrientation,
			ColorManagementMode.DoNotColorManage);

		using var output = new InMemoryRandomAccessStream();
		var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, output);
		encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, target, target, 96, 96, pixels.DetachPixelData());
		await encoder.FlushAsync();

		var bytes = new byte[output.Size];
		output.Seek(0);
		await output.ReadAsync(bytes.AsBuffer(), (uint)output.Size, InputStreamOptions.None);

		return new ProcessedPhoto(bytes, ContentType);
	}
}
