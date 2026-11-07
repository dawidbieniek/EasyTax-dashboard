using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Shell;

public sealed class PageRegistry : IPageRegistry
{
	private readonly Dictionary<Type, Type> _pageByViewModel = [];

	public void Add<TPage, TViewModel>()
		where TPage : Page
		where TViewModel : ViewModelBase
		=> _pageByViewModel[typeof(TViewModel)] = typeof(TPage);

	public Type GetPageType(Type viewModelType)
		=> _pageByViewModel.TryGetValue(viewModelType, out var pageType)
			? pageType
			: throw new InvalidOperationException($"No page registered for view model '{viewModelType.Name}'.");
}
