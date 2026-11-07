using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

public interface INavigationService
{
	/// <summary>Raised after navigation completes, with the type of the view model now displayed.</summary>
	event EventHandler<Type>? Navigated;

	/// <summary>Binds the service to the frame that hosts pages. Called once by the shell.</summary>
	void Initialize(Frame frame);

	bool NavigateTo<TViewModel>(object? parameter = null)
		where TViewModel : ViewModelBase;

	bool NavigateTo(Type viewModelType, object? parameter = null);
}
