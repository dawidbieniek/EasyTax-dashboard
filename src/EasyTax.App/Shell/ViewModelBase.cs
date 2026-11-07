using CommunityToolkit.Mvvm.ComponentModel;

namespace EasyTax.App.Shell;

public abstract class ViewModelBase : ObservableObject
{
	/// <summary>Called by the navigation service after the page showing this view model has loaded.</summary>
	public virtual Task OnNavigatedToAsync(object? parameter) => Task.CompletedTask;

	/// <summary>Called by the navigation service when the user leaves the page.</summary>
	public virtual void OnNavigatedFrom()
	{
	}
}
