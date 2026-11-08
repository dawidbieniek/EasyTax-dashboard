namespace EasyTax.App.Infrastructure;

/// <summary>Runs an action once the caller has been quiet for a while: each new request cancels the previous pending one.</summary>
internal sealed class Debouncer(TimeSpan delay)
{
	private CancellationTokenSource? _pending;

	/// <summary>Schedules the action. If another request arrives before the delay is over, this one is dropped.</summary>
	public async Task ScheduleAsync(Func<Task> action)
	{
		_pending?.Cancel();
		var current = _pending = new CancellationTokenSource();

		try
		{
			await Task.Delay(delay, current.Token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		await action();
	}
}
