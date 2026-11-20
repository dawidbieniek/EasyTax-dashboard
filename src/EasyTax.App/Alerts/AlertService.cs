using EasyTax.App.Infrastructure;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Core.Domain.Alerts;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;
using Microsoft.Extensions.Logging;

namespace EasyTax.App.Alerts;

internal sealed class AlertService : IAlertService
{
	// Every month input and selection change reaches the context; wait until the user has paused before reading everything again.
	private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(400);

	private readonly IClientRepository _clients;
	private readonly IMonthRecordRepository _months;
	private readonly IAlertAcknowledgementRepository _acknowledgements;
	private readonly ISettingsService _settings;
	private readonly ILogger<AlertService> _logger;
	private readonly Debouncer _debouncer = new(RefreshDelay);
	private readonly SemaphoreSlim _gate = new(1, 1);

	private AlertOptions _options;
	private List<string> _signature = [];

	public AlertService(IClientRepository clients, IMonthRecordRepository months, IAlertAcknowledgementRepository acknowledgements, IClientContext context, ISettingsService settings, ILogger<AlertService> logger)
	{
		_clients = clients;
		_months = months;
		_acknowledgements = acknowledgements;
		_settings = settings;
		_logger = logger;
		_options = settings.Current.Alerts;

		context.Changed += (_, _) => _ = _debouncer.ScheduleAsync(RefreshAsync);
		settings.Changed += OnSettingsChanged;
	}

	public IReadOnlyList<AlertItem> Items { get; private set; } = [];

	public int ActiveCount => Items.Count(item => !item.IsHandled);

	public event EventHandler? Changed;

	public async Task RefreshAsync()
	{
		await _gate.WaitAsync();
		try
		{
			var items = await EvaluateAsync();

			// Selecting another client or typing an unrelated value refreshes too; only tell listeners about real changes.
			var signature = items.Select(Describe).ToList();
			if (signature.SequenceEqual(_signature))
				return;

			_signature = signature;
			Items = items;
			Changed?.Invoke(this, EventArgs.Empty);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Could not evaluate the alerts");
		}
		finally
		{
			_gate.Release();
		}
	}

	public async Task AcknowledgeAsync(ClientAlert alert)
	{
		await _acknowledgements.AcknowledgeAsync(alert);
		await RefreshAsync();
	}

	public async Task UndoAcknowledgeAsync(ClientAlert alert)
	{
		await _acknowledgements.RemoveAsync(alert);
		await RefreshAsync();
	}

	private void OnSettingsChanged(object? sender, EventArgs e)
	{
		// Settings also change when the selection is remembered; only new thresholds matter here.
		var options = _settings.Current.Alerts;
		if (options == _options)
			return;

		_options = options;
		_ = _debouncer.ScheduleAsync(RefreshAsync);
	}

	private async Task<List<AlertItem>> EvaluateAsync()
	{
		var today = DateOnly.FromDateTime(DateTime.Today);
		var clients = await _clients.GetAllAsync();
		var recordsByClient = (await _months.GetAllForYearAsync(today.Year)).ToLookup(record => record.ClientId);
		var acknowledgements = await _acknowledgements.GetAllAsync();

		return
		[
			.. clients
				.SelectMany(client => AlertEvaluator.Evaluate(client, [.. recordsByClient[client.Id]], today, _options))
				.Select(alert => new AlertItem(alert, acknowledgements.Any(ack => ack.Covers(alert))))
				.OrderByDescending(item => item.Alert.Severity)
				.ThenBy(item => item.Alert.Client.LastName, StringComparer.CurrentCultureIgnoreCase)
				.ThenBy(item => item.Alert.Client.FirstName, StringComparer.CurrentCultureIgnoreCase)
				.ThenBy(item => item.Alert.Kind),
		];
	}

	private static string Describe(AlertItem item)
		=> $"{item.Alert.Client.Id}|{item.Alert.Kind}|{item.Alert.Severity}|{item.Alert.Year}|{item.IsHandled}|{item.Alert.Limit?.Income}|{item.Alert.DaysLeft}|{item.Alert.Client.Email}|{item.Alert.Client.FullName}";
}
