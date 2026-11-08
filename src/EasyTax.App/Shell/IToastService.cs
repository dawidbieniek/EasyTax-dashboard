using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

/// <summary>One message shown in the corner of the window.</summary>
public sealed class ToastItem(string message, InfoBarSeverity severity)
{
	public string Message { get; } = message;

	public InfoBarSeverity Severity { get; } = severity;
}

/// <summary>Short, self-closing feedback ("saved", "something went wrong") that does not interrupt the user.</summary>
public interface IToastService
{
	/// <summary>The messages currently on screen. The shell binds to this.</summary>
	ObservableCollection<ToastItem> Toasts { get; }

	void Success(string message);

	void Info(string message);

	void Warning(string message);

	void Error(string message);

	/// <summary>Closes a message early (the user pressed its close button).</summary>
	void Dismiss(ToastItem toast);
}
