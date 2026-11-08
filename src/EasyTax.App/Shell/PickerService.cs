using Windows.Storage.Pickers;
using WinRT.Interop;

namespace EasyTax.App.Shell;

public sealed class PickerService(IShellWindow shellWindow) : IPickerService
{
	public async Task<string?> PickFolderAsync()
	{
		var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
		picker.FileTypeFilter.Add("*");
		InitializeWithWindow.Initialize(picker, shellWindow.Handle);

		var folder = await picker.PickSingleFolderAsync();

		return folder?.Path;
	}

	public async Task<string?> PickFileAsync(string extension)
	{
		var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
		picker.FileTypeFilter.Add(extension);
		InitializeWithWindow.Initialize(picker, shellWindow.Handle);

		var file = await picker.PickSingleFileAsync();

		return file?.Path;
	}
}
