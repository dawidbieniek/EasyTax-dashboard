namespace EasyTax.App.Shell;

/// <summary>Gives services access to the main window's handle (needed by system pickers) without referencing the window.</summary>
public interface IShellWindow
{
	nint Handle { get; }

	void Attach(Microsoft.UI.Xaml.Window window);
}
