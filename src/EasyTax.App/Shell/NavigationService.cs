using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace EasyTax.App.Shell;

public sealed class NavigationService(IPageRegistry pages, ILogger<NavigationService> logger) : INavigationService
{
	private Frame? _frame;
	private ViewModelBase? _currentViewModel;

	public event EventHandler<ViewModelBase>? Navigated;

	public void Initialize(Frame frame)
	{
		_frame = frame;
		_frame.Navigated += OnFrameNavigated;
		_frame.NavigationFailed += OnFrameNavigationFailed;
	}

	public Task<bool> CanLeaveAsync() => _currentViewModel?.ConfirmLeaveAsync() ?? Task.FromResult(true);

	public bool NavigateTo<TViewModel>(object? parameter = null)
		where TViewModel : ViewModelBase
		=> NavigateTo(typeof(TViewModel), parameter);

	public bool NavigateTo(Type viewModelType, object? parameter = null)
	{
		var frame = _frame ?? throw new InvalidOperationException("NavigationService is not initialized.");
		var pageType = pages.GetPageType(viewModelType);

		if (frame.Content?.GetType() == pageType && parameter is null)
			return false;

		return frame.Navigate(pageType, parameter, new SuppressNavigationTransitionInfo());
	}

	private async void OnFrameNavigated(object sender, NavigationEventArgs e)
	{
		_currentViewModel?.OnNavigatedFrom();
		_currentViewModel = null;

		// Pages set their own DataContext to the view model in their constructor.
		if (e.Content is not FrameworkElement { DataContext: ViewModelBase viewModel })
			return;

		_currentViewModel = viewModel;
		Navigated?.Invoke(this, viewModel);

		try
		{
			await viewModel.OnNavigatedToAsync(e.Parameter);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "{ViewModel} failed while handling navigation.", viewModel.GetType().Name);
		}
	}

	private void OnFrameNavigationFailed(object sender, NavigationFailedEventArgs e)
		=> logger.LogError(e.Exception, "Navigation to {PageType} failed.", e.SourcePageType.Name);
}
