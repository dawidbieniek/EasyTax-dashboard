using Windows.Storage;

namespace EasyTax.App.Infrastructure;

/// <summary>
/// Well-known locations on disk.
/// The installed (MSIX) app keeps everything in its own package folder, so nothing it does mixes with files a development build
/// leaves in %LocalAppData%\EasyTax, and uninstalling it removes the data (backups live elsewhere, see <see cref="DefaultBackupFolder"/>).
/// An unpackaged development build uses %LocalAppData%\EasyTax.
/// The EASYTAX_DATA_DIR environment variable overrides both (handy for development and testing).
/// </summary>
internal static class AppPaths
{
	public static string DataDirectory { get; } = ChooseDataDirectory();

	public static string DatabaseFile { get; } = Path.Combine(DataDirectory, "easytax.db");

	/// <summary>The database key, wrapped with Windows DPAPI.</summary>
	public static string KeyFile { get; } = Path.Combine(DataDirectory, "easytax.key");

	public static string LogDirectory { get; } = Path.Combine(DataDirectory, "logs");

	public static string SettingsFile { get; } = Path.Combine(DataDirectory, "settings.json");

	/// <summary>Where backups go unless Settings says otherwise. In Documents, so an uninstall does not take them along.</summary>
	public static string DefaultBackupFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EasyTax", "Backups");

	private static string ChooseDataDirectory()
	{
		if (Environment.GetEnvironmentVariable("EASYTAX_DATA_DIR") is { Length: > 0 } overridden)
			return overridden;

		if (PackageInfo.IsPackaged)
			return ApplicationData.Current.LocalFolder.Path;

		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasyTax");
	}
}
