using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Localization;
using EasyTax.App.Shell;

namespace EasyTax.App.Alerts;

/// <summary>The clients that need attention, most urgent first. Handled alerts are hidden unless asked for.</summary>
public sealed partial class AlertsViewModel(
	IAlertService alerts,
	AlertDescriber describer,
	IClientContext context,
	INavigationService navigation,
	ILocalizer localizer) : ViewModelBase
{
	public ObservableCollection<AlertRow> Rows { get; } = [];

	[ObservableProperty]
	public partial bool ShowHandled { get; set; }

	[ObservableProperty]
	public partial string Summary { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool IsEmpty { get; set; }

	public override async Task OnNavigatedToAsync(object? parameter)
	{
		alerts.Changed += OnAlertsChanged;

		// The list is normally current already; this makes sure that, for example, a date change since the last look shows up.
		await alerts.RefreshAsync();
		Rebuild();
	}

	public override void OnNavigatedFrom() => alerts.Changed -= OnAlertsChanged;

	partial void OnShowHandledChanged(bool value) => Rebuild();

	[RelayCommand]
	private async Task OpenClientAsync(AlertRow? row)
	{
		if (row is null)
			return;

		await context.SelectClientAsync(row.ClientId);
		navigation.NavigateTo<Clients.ClientDetailsViewModel>();
	}

	[RelayCommand]
	private async Task ToggleHandledAsync(AlertRow? row)
	{
		if (row is null)
			return;

		if (row.IsHandled)
			await alerts.UndoAcknowledgeAsync(row.Alert);
		else
			await alerts.AcknowledgeAsync(row.Alert);
	}

	private void OnAlertsChanged(object? sender, EventArgs e) => Rebuild();

	private void Rebuild()
	{
		var items = ShowHandled ? alerts.Items : [.. alerts.Items.Where(item => !item.IsHandled)];

		Rows.Clear();
		foreach (var item in items)
		{
			var client = item.Alert.Client;
			Rows.Add(new AlertRow(
				item,
				client.Id,
				client.FullName,
				context.ClientPhotos.GetValueOrDefault(client.Id),
				describer.Title(item.Alert),
				describer.Detail(item.Alert),
				describer.Severity(item.Alert),
				localizer[item.IsHandled ? "Alerts_Unacknowledge" : "Alerts_Acknowledge"],
				!string.IsNullOrWhiteSpace(client.Email)));
		}

		IsEmpty = Rows.Count == 0;
		Summary = string.Format(CultureInfo.CurrentCulture, localizer["Alerts_Summary"], alerts.ActiveCount);
	}
}
