using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace EasyTax.App.Shell;

public sealed class ShellWindow : IShellWindow
{
	private Window? _window;

	public nint Handle => _window is null
		? throw new InvalidOperationException("ShellWindow is not attached to a window.")
		: WindowNative.GetWindowHandle(_window);

	public void Attach(Window window) => _window = window;
}
