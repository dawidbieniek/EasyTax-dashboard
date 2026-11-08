using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Clients;
using EasyTax.App.Infrastructure;
using EasyTax.App.Localization;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Limits;
using EasyTax.Core.Domain.Months;
using LiveChartsCore;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace EasyTax.App.Dashboard;

/// <summary>
/// Everything about the selected client for the selected month and year: limit gauges, income and invoice charts,
/// the month's inputs, the checklist and the client's notes. It only reads from and writes through the client context.
/// </summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
	private const int MonthsInYear = 12;

	private readonly IClientContext _context;
	private readonly INavigationService _navigation;
	private readonly ILocalizer _localizer;
	private readonly ISettingsService _settings;

	private readonly Debouncer _notesDebouncer = new(TimeSpan.FromMilliseconds(800));
	private readonly ObservableCollection<double> _incomeValues = new(new double[MonthsInYear]);
	private readonly ObservableCollection<int> _salesValues = new(new int[MonthsInYear]);
	private readonly ObservableCollection<int> _costValues = new(new int[MonthsInYear]);
	private readonly string[] _monthLabels;

	private AppTheme _theme;
	private ChartPalette _palette;
	private bool _refreshing;
	private Guid? _notesClientId;

	public DashboardViewModel(IClientContext context, INavigationService navigation, ILocalizer localizer, ISettingsService settings)
	{
		_context = context;
		_navigation = navigation;
		_localizer = localizer;
		_settings = settings;

		_theme = settings.Current.Theme;
		_palette = ChartPalette.Create(_theme);
		_monthLabels = [.. Enumerable.Range(1, MonthsInYear).Select(month => localizer[$"Month_{month}"])];

		VatGauge = new LimitGaugeViewModel(localizer, "Gauge_Vat_Title", "Gauge_Vat_NotApplicable");
		CashRegisterGauge = new LimitGaugeViewModel(localizer, "Gauge_CashRegister_Title", "Gauge_CashRegister_NotApplicable");
		ZusPlusGauge = new LimitGaugeViewModel(localizer, "Gauge_ZusPlus_Title", "Gauge_ZusPlus_NotApplicable");

		ClientChecklist = [.. new[] { ChecklistKey.VatR, ChecklistKey.Ceidg1, ChecklistKey.Zus, ChecklistKey.Invoiced, ChecklistKey.ZusDra, ChecklistKey.Tax }.Select(CreateChecklistItem)];
		MonthChecklist = [.. new[] { ChecklistKey.JpkV7, ChecklistKey.VatUe }.Select(CreateChecklistItem)];

		BuildCharts();

		_context.Changed += OnContextChanged;
		_settings.Changed += OnSettingsChanged;
		Refresh();
	}

	public override AppBarMode AppBarMode => AppBarMode.Full;

	public LimitGaugeViewModel VatGauge { get; }

	public LimitGaugeViewModel CashRegisterGauge { get; }

	public LimitGaugeViewModel ZusPlusGauge { get; }

	/// <summary>The per-client items. Created once; their state is updated in place.</summary>
	public IReadOnlyList<ChecklistItem> ClientChecklist { get; }

	/// <summary>The per-month items. Created once; their state is updated in place.</summary>
	public IReadOnlyList<ChecklistItem> MonthChecklist { get; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasNoClients))]
	public partial bool HasClients { get; set; }

	/// <summary>False for months before the client joined: the month's inputs and checklist are disabled.</summary>
	[ObservableProperty]
	public partial bool IsMonthEditable { get; set; }

	[ObservableProperty]
	public partial string MonthTitle { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string IncomeChartTitle { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string InvoicesChartTitle { get; set; } = string.Empty;

	// Inputs of the selected month (NumberBox values; NaN while the box is empty).
	[ObservableProperty]
	public partial double IncomeInput { get; set; }

	[ObservableProperty]
	public partial double SalesInput { get; set; }

	[ObservableProperty]
	public partial double CostInput { get; set; }

	[ObservableProperty]
	public partial string Notes { get; set; } = string.Empty;

	// False while the selected year has no income / no invoices: that chart then shows a hint instead of empty axes.
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasNoIncomeData))]
	public partial bool HasIncomeData { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasNoInvoiceData))]
	public partial bool HasInvoiceData { get; set; }

	// Chart definitions. Rebuilt when the theme changes; the values live in the collections above and update in place.
	[ObservableProperty]
	public partial ISeries[] IncomeSeries { get; set; } = [];

	[ObservableProperty]
	public partial ISeries[] InvoiceSeries { get; set; } = [];

	[ObservableProperty]
	public partial ICartesianAxis[] MonthAxes { get; set; } = [];

	[ObservableProperty]
	public partial ICartesianAxis[] IncomeValueAxes { get; set; } = [];

	[ObservableProperty]
	public partial ICartesianAxis[] InvoiceValueAxes { get; set; } = [];

	[ObservableProperty]
	public partial SolidColorPaint LegendPaint { get; set; } = new(SKColors.Gray);

	[ObservableProperty]
	public partial SolidColorPaint TooltipTextPaint { get; set; } = new(SKColors.White);

	[ObservableProperty]
	public partial SolidColorPaint TooltipBackgroundPaint { get; set; } = new(SKColors.Black);

	public bool HasNoClients => !HasClients;

	public bool HasNoIncomeData => !HasIncomeData;

	public bool HasNoInvoiceData => !HasInvoiceData;

	public override void OnNavigatedFrom()
	{
		_context.Changed -= OnContextChanged;
		_settings.Changed -= OnSettingsChanged;
	}

	partial void OnIncomeInputChanged(double value) => SaveInputs();

	partial void OnSalesInputChanged(double value) => SaveInputs();

	partial void OnCostInputChanged(double value) => SaveInputs();

	partial void OnNotesChanged(string value)
	{
		if (_refreshing || _notesClientId is not { } clientId)
			return;

		// The pending save carries the client's id, so it still lands on the right client if the selection moves meanwhile.
		_ = _notesDebouncer.ScheduleAsync(() => _context.UpdateClientAsync(clientId, client => client.Notes = value, notify: false));
	}

	[RelayCommand]
	private void GoToClients() => _navigation.NavigateTo<ClientsViewModel>();

	[RelayCommand]
	private Task ToggleChecklistAsync(ChecklistKey key) => key switch
	{
		ChecklistKey.JpkV7 => _context.UpdateCurrentRecordAsync(record => record.JpkV7DoneAt = Toggled(record.JpkV7DoneAt)),
		ChecklistKey.VatUe => _context.UpdateCurrentRecordAsync(record => record.VatUeDoneAt = Toggled(record.VatUeDoneAt)),
		_ => ToggleClientItemAsync(key),
	};

	private Task ToggleClientItemAsync(ChecklistKey key)
	{
		if (_context.SelectedClient is not { } selected)
			return Task.CompletedTask;

		return _context.UpdateClientAsync(selected.Id, client =>
		{
			switch (key)
			{
				case ChecklistKey.VatR:
					client.VatRDoneAt = Toggled(client.VatRDoneAt);
					break;
				case ChecklistKey.Ceidg1:
					client.Ceidg1DoneAt = Toggled(client.Ceidg1DoneAt);
					break;
				case ChecklistKey.Zus:
					client.ZusDoneAt = Toggled(client.ZusDoneAt);
					break;
				case ChecklistKey.Invoiced:
					client.InvoicedAt = Toggled(client.InvoicedAt);
					break;
				case ChecklistKey.ZusDra:
					client.ZusDraDoneAt = Toggled(client.ZusDraDoneAt);
					break;
				case ChecklistKey.Tax:
					client.TaxDoneAt = Toggled(client.TaxDoneAt);
					break;
			}
		});
	}

	private void SaveInputs()
	{
		if (_refreshing || !IsMonthEditable)
			return;

		var income = ToAmount(IncomeInput);
		var sales = ToCount(SalesInput);
		var cost = ToCount(CostInput);

		var record = _context.CurrentRecord;
		var unchanged = (record?.Income ?? 0) == income && (record?.SalesInvoices ?? 0) == sales && (record?.CostInvoices ?? 0) == cost;
		if (unchanged)
			return;

		_ = _context.UpdateCurrentRecordAsync(current =>
		{
			current.Income = income;
			current.SalesInvoices = sales;
			current.CostInvoices = cost;
		});
	}

	private void OnContextChanged(object? sender, EventArgs e) => Refresh();

	private void OnSettingsChanged(object? sender, EventArgs e)
	{
		// Settings also change when the selection is remembered; only a different theme needs new chart colours.
		if (_settings.Current.Theme == _theme)
			return;

		_theme = _settings.Current.Theme;
		_palette = ChartPalette.Create(_theme);
		BuildCharts();
		Refresh();
	}

	private void Refresh()
	{
		_refreshing = true;
		try
		{
			var client = _context.SelectedClient;
			HasClients = client is not null;
			if (client is null)
				return;

			var record = _context.CurrentRecord;
			IsMonthEditable = client.IsMonthSelectable(_context.SelectedMonth);
			MonthTitle = Capitalize(_context.SelectedMonth.ToString("MMMM yyyy", ClientFormatting.Culture));

			RefreshGauges(client);
			RefreshCharts();

			IncomeInput = (double)(record?.Income ?? 0);
			SalesInput = record?.SalesInvoices ?? 0;
			CostInput = record?.CostInvoices ?? 0;

			// Notes are only reloaded when another client is shown, so a refresh never overwrites what is being typed.
			if (_notesClientId != client.Id)
			{
				_notesClientId = client.Id;
				Notes = client.Notes;
			}

			ApplyChecklist(client, record);
		}
		finally
		{
			_refreshing = false;
		}
	}

	private void RefreshGauges(Client client)
	{
		foreach (var status in LimitCalculator.CalculateAll(client, _context.YearRecords.ToList(), _context.Year))
		{
			var gauge = status.Type switch
			{
				LimitType.Vat => VatGauge,
				LimitType.CashRegister => CashRegisterGauge,
				_ => ZusPlusGauge,
			};

			gauge.Update(status, _palette);
		}
	}

	private void RefreshCharts()
	{
		for (var month = 1; month <= MonthsInYear; month++)
		{
			var record = _context.YearRecords.FirstOrDefault(r => r.Month.Month == month);

			SetIfChanged(_incomeValues, month - 1, (double)(record?.Income ?? 0));
			SetIfChanged(_salesValues, month - 1, record?.SalesInvoices ?? 0);
			SetIfChanged(_costValues, month - 1, record?.CostInvoices ?? 0);
		}

		HasIncomeData = _incomeValues.Any(v => v != 0);
		HasInvoiceData = _salesValues.Any(v => v != 0) || _costValues.Any(v => v != 0);

		IncomeChartTitle = string.Format(CultureInfo.CurrentCulture, _localizer["Dashboard_IncomeChart_Title"], _context.Year);
		InvoicesChartTitle = string.Format(CultureInfo.CurrentCulture, _localizer["Dashboard_InvoicesChart_Title"], _context.Year);
	}

	/// <summary>Creates the series and axes with the current palette. The series keep pointing at the same value collections.</summary>
	private void BuildCharts()
	{
		var accent = new SolidColorPaint(_palette.Accent) { StrokeThickness = 3 };

		IncomeSeries =
		[
			new LineSeries<double>
			{
				Name = _localizer["Dashboard_Series_Income"],
				Values = _incomeValues,
				Stroke = accent,
				Fill = new SolidColorPaint(_palette.Accent.WithAlpha(40)),
				GeometrySize = 8,
				GeometryStroke = accent,
				GeometryFill = new SolidColorPaint(_palette.Accent),
				LineSmoothness = 0.5,
			},
		];

		InvoiceSeries =
		[
			new ColumnSeries<int>
			{
				Name = _localizer["Dashboard_Series_Sales"],
				Values = _salesValues,
				Fill = new SolidColorPaint(_palette.Accent),
			},
			new ColumnSeries<int>
			{
				Name = _localizer["Dashboard_Series_Costs"],
				Values = _costValues,
				Fill = new SolidColorPaint(_palette.Secondary),
			},
		];

		MonthAxes = [new Axis { Labels = _monthLabels, LabelsPaint = new SolidColorPaint(_palette.Label), ShowSeparatorLines = false }];

		IncomeValueAxes =
		[
			new Axis
			{
				MinLimit = 0,
				MinStep = 1,
				Labeler = value => value.ToString("N0", ClientFormatting.Culture),
				LabelsPaint = new SolidColorPaint(_palette.Label),
				SeparatorsPaint = new SolidColorPaint(_palette.Separator) { StrokeThickness = 1 },
			},
		];

		InvoiceValueAxes =
		[
			new Axis
			{
				MinLimit = 0,
				MinStep = 1,
				Labeler = value => value.ToString("N0", ClientFormatting.Culture),
				LabelsPaint = new SolidColorPaint(_palette.Label),
				SeparatorsPaint = new SolidColorPaint(_palette.Separator) { StrokeThickness = 1 },
			},
		];

		LegendPaint = new SolidColorPaint(_palette.Label);
		TooltipTextPaint = new SolidColorPaint(new SKColor(235, 235, 235));
		TooltipBackgroundPaint = new SolidColorPaint(new SKColor(40, 40, 40, 240));
	}

	private ChecklistItem CreateChecklistItem(ChecklistKey key) => new(key, _localizer.Label(key));

	private void ApplyChecklist(Client client, MonthRecord? record)
	{
		var culture = ClientFormatting.Culture;

		ClientChecklist[0].Set(client.VatRDoneAt, culture);
		ClientChecklist[1].Set(client.Ceidg1DoneAt, culture);
		ClientChecklist[2].Set(client.ZusDoneAt, culture);
		ClientChecklist[3].Set(client.InvoicedAt, culture);
		ClientChecklist[4].Set(client.ZusDraDoneAt, culture);
		ClientChecklist[5].Set(client.TaxDoneAt, culture);

		MonthChecklist[0].Set(record?.JpkV7DoneAt, culture);
		MonthChecklist[1].Set(record?.VatUeDoneAt, culture);
	}

	/// <summary>Writing an unchanged value would still make the chart redraw and re-animate.</summary>
	private static void SetIfChanged<T>(ObservableCollection<T> values, int index, T value)
		where T : IEquatable<T>
	{
		if (!values[index].Equals(value))
			values[index] = value;
	}

	private static DateTime? Toggled(DateTime? value) => value is null ? DateTime.Now : null;

	private static decimal ToAmount(double value) => double.IsNaN(value) ? 0 : Math.Round((decimal)Math.Max(value, 0), 2);

	private static int ToCount(double value) => double.IsNaN(value) ? 0 : (int)Math.Max(Math.Round(value), 0);

	private static string Capitalize(string text) => text.Length == 0 ? text : char.ToUpper(text[0], ClientFormatting.Culture) + text[1..];
}
