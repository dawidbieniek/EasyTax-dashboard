using System.Globalization;
using EasyTax.App.Infrastructure;
using EasyTax.App.Localization;
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
		// Set here, not in an async method: a context set inside one is reverted when its first segment ends.
		SynchronizationContext.SetSynchronizationContext(new ErrorHandlingSynchronizationContext(SynchronizationContext.Current!, ReportError));

		UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnTaskException;
		AppDomain.CurrentDomain.UnhandledException += OnDomainException;
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
		try
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
		}
		catch (Exception ex)
		{
			ShowStartupFailure(ex);
			return;
		}

		_window = Services.GetRequiredService<MainWindow>();
		_window.Closed += OnWindowClosed;
		_window.Activate();
	}

	private void ShowStartupFailure(Exception exception)
	{
		Services.GetRequiredService<ILogger<App>>().LogCritical(exception, "The app could not start");

		var localizer = Services.GetRequiredService<ILocalizer>();
		var details = string.Format(CultureInfo.CurrentCulture, localizer["Startup_LogFile"], AppPaths.LogDirectory);
		new StartupErrorWindow(localizer["Startup_Title"], localizer["Startup_Message"], details, localizer["Startup_Close"]).Activate();
	}

	private async void OnWindowClosed(object sender, WindowEventArgs args)
	{
		await _host.StopAsync();
		_host.Dispose();
	}

	// Errors raised by event handlers: log them, tell the user, keep the app running.
	private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
	{
		e.Handled = true;
		ReportError(e.Exception);
	}

	// Also reached for errors thrown after an await in an async void method (see ErrorHandlingSynchronizationContext).
	private void ReportError(Exception exception)
	{
		Services.GetRequiredService<ILogger<App>>().LogError(exception, "Unhandled exception");

		try
		{
			var localizer = Services.GetRequiredService<ILocalizer>();
			Services.GetRequiredService<IToastService>().Error(UserFacingErrors.Describe(exception, localizer));
		}
		catch (Exception ex)
		{
			// Nothing left to show the message with; the first error is already in the log.
			Services.GetRequiredService<ILogger<App>>().LogError(ex, "Could not show the error message");
		}
	}

	private void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
		=> Services.GetRequiredService<ILogger<App>>().LogError(e.Exception, "Unobserved task exception");

	private void OnDomainException(object sender, System.UnhandledExceptionEventArgs e)
		=> Services.GetRequiredService<ILogger<App>>().LogCritical(e.ExceptionObject as Exception, "Fatal exception");
}
