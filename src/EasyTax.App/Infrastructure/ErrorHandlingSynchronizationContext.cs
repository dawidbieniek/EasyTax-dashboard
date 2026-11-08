namespace EasyTax.App.Infrastructure;

/// <summary>
/// Wraps the UI thread's synchronization context. An exception that escapes an <c>async void</c> method after its first
/// await is re-thrown through this context, and WinUI answers that with a process crash instead of raising
/// <c>Application.UnhandledException</c>. Catching it here lets the app log it, show a message and carry on.
/// </summary>
internal sealed class ErrorHandlingSynchronizationContext(SynchronizationContext inner, Action<Exception> onError) : SynchronizationContext
{
	public override void Post(SendOrPostCallback d, object? state) => inner.Post(_ => Run(d, state), null);

	public override void Send(SendOrPostCallback d, object? state) => inner.Send(_ => Run(d, state), null);

	public override SynchronizationContext CreateCopy() => new ErrorHandlingSynchronizationContext(inner.CreateCopy(), onError);

	private void Run(SendOrPostCallback d, object? state)
	{
		try
		{
			d(state);
		}
		catch (Exception ex)
		{
			onError(ex);
		}
	}
}
