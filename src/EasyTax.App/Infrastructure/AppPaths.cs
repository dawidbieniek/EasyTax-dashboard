namespace EasyTax.App.Infrastructure;

/// <summary>Well-known locations on disk. Unpackaged app, so everything lives under %LocalAppData%\EasyTax.</summary>
internal static class AppPaths
{
	public static string DataDirectory { get; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"EasyTax");

	public static string DatabaseFile { get; } = Path.Combine(DataDirectory, "easytax.db");

	public static string SettingsFile { get; } = Path.Combine(DataDirectory, "settings.json");
}
