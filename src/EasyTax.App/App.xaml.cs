using EasyTax.App.Infrastructure;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Core.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace EasyTax.App;

public partial class App : Application
{
	private readonly IHost _host;
	private Window? _window;

	public App()
	{
		InitializeComponent();

		_host = AppHost.Build();
		UnhandledException += OnUnhandledException;
	}

	/// <summary>
	/// Service locator used only where the framework creates objects for us (pages created by <c>Frame</c>).
	/// Everything else receives its dependencies through constructors.
	/// </summary>
	public static IServiceProvider Services => ((App)Current)._host.Services;

	public static T GetService<T>()
		where T : notnull
		=> Services.GetRequiredService<T>();

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		await _host.StartAsync();

		await Services.GetRequiredService<ISettingsService>().LoadAsync();
		await Services.GetRequiredService<IDatabaseInitializer>().InitializeAsync();

#if DEBUG
		// Development helper: `EasyTax.exe --seed-demo` fills an empty database with sample clients.
		if (Environment.GetCommandLineArgs().Contains("--seed-demo"))
			await Services.GetRequiredService<DemoDataSeeder>().SeedAsync();
#endif

		await Services.GetRequiredService<IClientContext>().InitializeAsync();

		_window = Services.GetRequiredService<MainWindow>();
		_window.Closed += OnWindowClosed;
		_window.Activate();
	}

	private async void OnWindowClosed(object sender, WindowEventArgs args)
	{
		await _host.StopAsync();
		_host.Dispose();
	}

	private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		=> Services.GetRequiredService<ILogger<App>>().LogCritical(e.Exception, "Unhandled exception: {Message}", e.Message);
}
