using EasyTax.Core.Domain.Months;

namespace EasyTax.App.Shell;

/// <summary>One button of the month strip.</summary>
/// <param name="Month">1–12.</param>
/// <param name="Label">Short month name.</param>
/// <param name="State">Decides the colour: accounted (green), not accounted (orange), before join (grey, disabled).</param>
/// <param name="IsSelected">Whether this is the selected month.</param>
public sealed record MonthItem(int Month, string Label, MonthState State, bool IsSelected)
{
	public bool IsEnabled => State != MonthState.BeforeJoin;
}
