using EasyTax.App.Clients;
using EasyTax.App.Dashboard;
using EasyTax.App.Localization;
using EasyTax.App.Photos;
using EasyTax.App.Security;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

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

		// Logging: a file per day under the data folder, two weeks kept. Entity Framework logs every SQL command at
		// Information level, which would drown everything else, so it is limited to warnings.
		var logger = new LoggerConfiguration()
			.MinimumLevel.Information()
			.WriteTo.File(Path.Combine(AppPaths.LogDirectory, "easytax-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
			.CreateLogger();
		builder.Logging.AddSerilog(logger, dispose: true);
		builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

		var services = builder.Services;

		// Persistence (EasyTax.Data)
		services.AddEasyTaxData(new EasyTaxDataOptions(AppPaths.DatabaseFile));

		// App services
		services.AddSingleton<ILocalizer, Localizer>();
		services.AddSingleton(_ => new DatabaseKeyService(AppPaths.KeyFile));
		services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(AppPaths.SettingsFile));
		services.AddSingleton<IThemeService, ThemeService>();
		services.AddSingleton<INavigationService, NavigationService>();
		services.AddSingleton<IClientContext, ClientContext>();
		services.AddSingleton<IDialogService, DialogService>();
		services.AddSingleton<IToastService, ToastService>();
		services.AddSingleton<IShellWindow, ShellWindow>();
		services.AddSingleton<IPhotoService, PhotoService>();
		services.AddTransient<AddClientViewModel>();

		// Shell
		services.AddSingleton<AppBarViewModel>();
		services.AddSingleton<MainWindow>();

		// Pages <-> view models. Add one line here per screen.
		var pages = new PageRegistry();
		services.AddSingleton<IPageRegistry>(pages);
		services.AddPage<DashboardPage, DashboardViewModel>(pages);
		services.AddPage<ClientsPage, ClientsViewModel>(pages);
		services.AddPage<ClientDetailsPage, ClientDetailsViewModel>(pages);
		services.AddPage<SettingsPage, SettingsViewModel>(pages);

#if DEBUG
		services.AddTransient<DemoDataSeeder>();
#endif

		return builder.Build();
	}
}
