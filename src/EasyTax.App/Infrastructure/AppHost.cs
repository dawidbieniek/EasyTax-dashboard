using EasyTax.App.Clients;
using EasyTax.App.Dashboard;
using EasyTax.App.Localization;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyTax.App.Infrastructure;

/// <summary>Composition root: builds the generic host that owns DI, configuration and logging.</summary>
internal static class AppHost
{
	public static IHost Build()
	{
		var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
		{
			ContentRootPath = AppContext.BaseDirectory,
		});

		var services = builder.Services;

		// Persistence (EasyTax.Data)
		services.AddEasyTaxData(new EasyTaxDataOptions(AppPaths.DatabaseFile));

		// App services
		services.AddSingleton<ILocalizer, Localizer>();
		services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(AppPaths.SettingsFile));
		services.AddSingleton<IThemeService, ThemeService>();
		services.AddSingleton<INavigationService, NavigationService>();
		services.AddSingleton<IClientContext, ClientContext>();

		// Shell
		services.AddSingleton<AppBarViewModel>();
		services.AddSingleton<MainWindow>();

		// Pages <-> view models. Add one line here per screen.
		var pages = new PageRegistry();
		services.AddSingleton<IPageRegistry>(pages);
		services.AddPage<DashboardPage, DashboardViewModel>(pages);
		services.AddPage<ClientsPage, ClientsViewModel>(pages);
		services.AddPage<SettingsPage, SettingsViewModel>(pages);

#if DEBUG
		services.AddTransient<DemoDataSeeder>();
#endif

		return builder.Build();
	}
}
