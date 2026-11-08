using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyTax.App.Clients;
using EasyTax.App.Localization;
using EasyTax.Core.Domain.Limits;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace EasyTax.App.Dashboard;

/// <summary>One gauge card: how far a client is towards an income limit. Updated in place so its bindings stay alive.</summary>
public sealed partial class LimitGaugeViewModel(ILocalizer localizer, string titleKey, string notApplicableKey) : ObservableObject
{
	private const double ArcWidth = 14;

	// The arc's value is a live object: changing it animates the existing arc instead of building a new gauge.
	private readonly ObservableValue _arcValue = new(0);
	private SKColor? _arcFill;
	private SKColor? _arcTrack;

	public string Title { get; } = localizer[titleKey];

	public string NotApplicableText { get; } = localizer[notApplicableKey];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsNotApplicable))]
	public partial bool IsApplicable { get; set; }

	[ObservableProperty]
	public partial LimitState State { get; set; }

	/// <summary>Colour of the percentage and the state text: neutral, amber or red, matching the arc and the current theme.</summary>
	[ObservableProperty]
	public partial Windows.UI.Color StateColor { get; set; }

	[ObservableProperty]
	public partial string IncomeText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string LimitText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PercentText { get; set; } = string.Empty;

	/// <summary>"Close to the limit" / "Over the limit", empty while everything is fine.</summary>
	[ObservableProperty]
	public partial string StateText { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Tooltip { get; set; } = string.Empty;

	[ObservableProperty]
	public partial IEnumerable<ISeries> Series { get; set; } = [];

	public bool IsNotApplicable => !IsApplicable;

	public void Update(LimitStatus status, ChartPalette palette)
	{
		var percent = (double)(status.Ratio * 100);

		IsApplicable = status.IsApplicable;
		State = status.State;
		IncomeText = ClientFormatting.Money(status.Income);
		LimitText = string.Format(CultureInfo.CurrentCulture, localizer["Gauge_LimitOf"], ClientFormatting.Money(status.Limit));
		PercentText = $"{Math.Round(percent):0}%";
		StateText = status.State switch
		{
			LimitState.Warning => localizer["Gauge_State_Warning"],
			LimitState.Over => localizer["Gauge_State_Over"],
			_ => string.Empty,
		};
		Tooltip = string.Format(
			CultureInfo.CurrentCulture,
			localizer["Gauge_CountedFrom"],
			ClientFormatting.Date(status.CountedFrom),
			ClientFormatting.Date(status.CountedTo));

		var fill = status.State switch
		{
			LimitState.Warning => palette.Warning,
			LimitState.Over => palette.Danger,
			_ => palette.Accent,
		};

		StateColor = status.State switch
		{
			LimitState.Warning => ToUi(palette.Warning),
			LimitState.Over => ToUi(palette.Danger),
			_ => ToUi(palette.Label),
		};

		var arcValue = Math.Min(percent, 100);
		if (_arcValue.Value != arcValue)
			_arcValue.Value = arcValue;

		// A new arc is only built when its colours change (state or theme); otherwise the value change above animates the existing one.
		if (_arcFill == fill && _arcTrack == palette.Track)
			return;

		_arcFill = fill;
		_arcTrack = palette.Track;
		Series = [.. BuildArc(_arcValue, fill, palette.Track)];
	}

	private static Windows.UI.Color ToUi(SKColor color) => Windows.UI.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);

	private static IEnumerable<ISeries> BuildArc(ObservableValue value, SKColor fill, SKColor track)
		=> GaugeGenerator.BuildSolidGauge(
			new GaugeItem(value, series =>
			{
				series.Fill = new SolidColorPaint(fill);
				series.MaxRadialColumnWidth = ArcWidth;
				series.CornerRadius = ArcWidth / 2;
				series.DataLabelsPaint = null;
			}),
			new GaugeItem(GaugeItem.Background, series =>
			{
				series.Fill = new SolidColorPaint(track);
				series.MaxRadialColumnWidth = ArcWidth;
				series.CornerRadius = ArcWidth / 2;
				series.DataLabelsPaint = null;
			}));
}
