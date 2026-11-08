using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace EasyTax.App.Infrastructure;

/// <summary>
/// The app's top-level windows, in the order they were opened (start-up windows such as the recovery key, then the main window,
/// and any key window opened later). A second launch of the app is handed to the running copy, which uses this to bring
/// the window the user is actually working in to the front, whichever one that is.
/// </summary>
internal static class WindowStack
{
	private static readonly Lock Gate = new();
	private static readonly List<Window> Windows = [];
	private static DispatcherQueue? _dispatcher;

	/// <summary>Registers a window as the topmost one until it is closed. Call on the UI thread.</summary>
	public static void Push(Window window)
	{
		lock (Gate)
		{
			_dispatcher ??= DispatcherQueue.GetForCurrentThread();
			Windows.Add(window);
		}

		window.Closed += (_, _) =>
		{
			lock (Gate)
				Windows.Remove(window);
		};
	}

	/// <summary>Restores (if minimized) and activates the topmost window. May be called from any thread.</summary>
	public static void BringToFront()
	{
		Window? top;
		DispatcherQueue? dispatcher;
		lock (Gate)
		{
			top = Windows.Count > 0 ? Windows[^1] : null;
			dispatcher = _dispatcher;
		}

		if (top is null || dispatcher is null)
			return;

		dispatcher.TryEnqueue(() =>
		{
			if (top.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized } presenter)
				presenter.Restore();

			top.Activate();
		});
	}
}
