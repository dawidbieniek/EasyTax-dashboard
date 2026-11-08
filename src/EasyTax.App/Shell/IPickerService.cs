namespace EasyTax.App.Shell;

/// <summary>Opens the system file and folder pickers over the main window, so view models never touch UI objects.</summary>
public interface IPickerService
{
	/// <summary>Lets the user choose a folder. Null when cancelled.</summary>
	Task<string?> PickFolderAsync();

	/// <summary>Lets the user choose one file with the given extension (for example <c>.zip</c>). Null when cancelled.</summary>
	Task<string?> PickFileAsync(string extension);
}
