using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

public interface INavigationService
{
	/// <summary>Raised after navigation completes, with the view model now displayed.</summary>
	event EventHandler<ViewModelBase>? Navigated;

	/// <summary>Binds the service to the frame that hosts pages. Called once by the shell.</summary>
	void Initialize(Frame frame);

	/// <summary>Asks the page that is currently shown whether it may be left. Call before navigating on the user's behalf.</summary>
	Task<bool> CanLeaveAsync();

	bool NavigateTo<TViewModel>(object? parameter = null)
		where TViewModel : ViewModelBase;

	bool NavigateTo(Type viewModelType, object? parameter = null);
}
