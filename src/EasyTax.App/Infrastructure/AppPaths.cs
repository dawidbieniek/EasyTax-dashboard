namespace EasyTax.App.Infrastructure;

/// <summary>
/// Well-known locations on disk. Unpackaged app, so everything lives under %LocalAppData%\EasyTax,
/// unless the EASYTAX_DATA_DIR environment variable points somewhere else (handy for development and testing).
/// </summary>
internal static class AppPaths
{
	public static string DataDirectory { get; } = Environment.GetEnvironmentVariable("EASYTAX_DATA_DIR") is { Length: > 0 } overridden
		? overridden
		: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyTax");

	public static string DatabaseFile { get; } = Path.Combine(DataDirectory, "easytax.db");

	public static string SettingsFile { get; } = Path.Combine(DataDirectory, "settings.json");
}
