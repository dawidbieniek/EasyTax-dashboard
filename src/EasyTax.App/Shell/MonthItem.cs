using CommunityToolkit.Mvvm.ComponentModel;
using EasyTax.Core.Domain.Months;

namespace EasyTax.App.Shell;

/// <summary>
/// One button of the month strip. Created once per month and updated in place, so its button is never re-created
/// when the state changes (re-creating buttons shows as flicker).
/// </summary>
public sealed partial class MonthItem(int month, string label) : ObservableObject
{
	/// <summary>1–12.</summary>
	public int Month { get; } = month;

	/// <summary>Short month name.</summary>
	public string Label { get; } = label;

	/// <summary>Decides the colour: accounted (green), not accounted (orange), before the client joined (grey, disabled).</summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsAccounted), nameof(IsNotAccounted), nameof(IsBeforeJoin), nameof(IsEnabled))]
	public partial MonthState State { get; set; } = MonthState.BeforeJoin;

	[ObservableProperty]
	public partial bool IsSelected { get; set; }

	public bool IsAccounted => State == MonthState.Accounted;

	public bool IsNotAccounted => State == MonthState.NotAccounted;

	public bool IsBeforeJoin => State == MonthState.BeforeJoin;

	public bool IsEnabled => State != MonthState.BeforeJoin;

	public void Set(MonthState state, bool isSelected)
	{
		State = state;
		IsSelected = isSelected;
	}
}
