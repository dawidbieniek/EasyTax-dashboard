using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

/// <summary>Keeps the list of toasts. Must be created on the UI thread; may be called from any thread.</summary>
public sealed class ToastService : IToastService
{
	private const int MaxVisible = 4;

	private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

	public ObservableCollection<ToastItem> Toasts { get; } = [];

	public void Success(string message) => Show(message, InfoBarSeverity.Success, TimeSpan.FromSeconds(4));

	public void Info(string message) => Show(message, InfoBarSeverity.Informational, TimeSpan.FromSeconds(4));

	public void Warning(string message) => Show(message, InfoBarSeverity.Warning, TimeSpan.FromSeconds(6));

	public void Error(string message) => Show(message, InfoBarSeverity.Error, TimeSpan.FromSeconds(8));

	public void Dismiss(ToastItem toast) => Toasts.Remove(toast);

	private void Show(string message, InfoBarSeverity severity, TimeSpan duration)
	{
		if (!_dispatcher.HasThreadAccess)
		{
			_dispatcher.TryEnqueue(() => Show(message, severity, duration));
			return;
		}

		var toast = new ToastItem(message, severity);
		Toasts.Add(toast);
		while (Toasts.Count > MaxVisible)
			Toasts.RemoveAt(0);

		_ = CloseAfterAsync(toast, duration);
	}

	private async Task CloseAfterAsync(ToastItem toast, TimeSpan duration)
	{
		await Task.Delay(duration);
		_dispatcher.TryEnqueue(() => Toasts.Remove(toast));
	}
}
