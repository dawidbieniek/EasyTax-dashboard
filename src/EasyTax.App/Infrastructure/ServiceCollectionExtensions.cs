using EasyTax.App.Shell;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Infrastructure;

internal static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		/// <summary>Registers a view model and records which page displays it.</summary>
		public IServiceCollection AddPage<TPage, TViewModel>(PageRegistry registry)
			where TPage : Page
			where TViewModel : ViewModelBase
		{
			registry.Add<TPage, TViewModel>();
			return services.AddTransient<TViewModel>();
		}
	}
}
