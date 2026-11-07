using EasyTax.Core.Domain.Months;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

/// <summary>Picks the month button template by the month's state, so each state can have its own colour.</summary>
public sealed class MonthTemplateSelector : DataTemplateSelector
{
	public DataTemplate Accounted { get; set; } = null!;

	public DataTemplate NotAccounted { get; set; } = null!;

	public DataTemplate BeforeJoin { get; set; } = null!;

	protected override DataTemplate SelectTemplateCore(object item)
		=> item is MonthItem month
			? month.State switch
			{
				MonthState.Accounted => Accounted,
				MonthState.NotAccounted => NotAccounted,
				_ => BeforeJoin,
			}
			: base.SelectTemplateCore(item);

	protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
		=> SelectTemplateCore(item);
}
