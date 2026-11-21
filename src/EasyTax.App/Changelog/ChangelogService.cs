using System.Reflection;
using EasyTax.App.Localization;
using EasyTax.App.Settings;
using EasyTax.App.Shell;
using EasyTax.Core.Domain.Changelog;
using Microsoft.Extensions.Logging;

namespace EasyTax.App.Changelog;

public sealed class ChangelogService(ISettingsService settings, IDialogService dialogs, ILocalizer localizer, ILogger<ChangelogService> logger) : IChangelogService
{
	private const string FileName = "changelog.json";

	public async Task ShowNewAsync()
	{
		var current = CurrentVersion();
		var lastSeen = Version.TryParse(settings.Current.LastSeenVersion, out var seen) ? seen : null;

		// Remembered up front: a window that fails to show must not come back on every start.
		settings.Update(s => s with { LastSeenVersion = current.ToString(3) });

		var releases = ChangelogReader.Since(Load(), lastSeen, current);
		if (releases.Count > 0)
			await dialogs.ShowChangelogAsync(localizer["Changelog_Title_New"], releases, localizer["Changelog_Close"]);
	}

	public async Task ShowAllAsync()
	{
		var releases = Load();
		if (releases.Count > 0)
			await dialogs.ShowChangelogAsync(localizer["Changelog_Title_All"], releases, localizer["Changelog_Close"]);
	}

	/// <summary>The running version, taken from the assembly (set from <c>&lt;Version&gt;</c> in Directory.Build.props).</summary>
	public static Version CurrentVersion()
	{
		var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

		return new Version(version.Major, version.Minor, Math.Max(version.Build, 0));
	}

	private IReadOnlyList<ChangelogRelease> Load()
	{
		try
		{
			var path = Path.Combine(AppContext.BaseDirectory, FileName);

			return File.Exists(path) ? ChangelogReader.Parse(File.ReadAllText(path)) : [];
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
		{
			// The changelog is a courtesy; a broken file must never stop the app or show an error at startup.
			logger.LogWarning(ex, "The changelog could not be read");

			return [];
		}
	}
}
