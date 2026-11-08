using CommunityToolkit.Mvvm.ComponentModel;

namespace EasyTax.App.Dashboard;

/// <summary>One button of the checklist. Created once and updated in place, so its button never has to be re-created.</summary>
public sealed partial class ChecklistItem(ChecklistKey key, string label) : ObservableObject
{
	public ChecklistKey Key { get; } = key;

	public string Label { get; } = label;

	[ObservableProperty]
	public partial bool IsDone { get; set; }

	/// <summary>When the item was checked off (day.month hour:minute), empty when not done.</summary>
	[ObservableProperty]
	public partial string Timestamp { get; set; } = string.Empty;

	public void Set(DateTime? doneAt, IFormatProvider culture)
	{
		IsDone = doneAt is not null;
		Timestamp = doneAt?.ToString("dd.MM HH:mm", culture) ?? string.Empty;
	}
}
