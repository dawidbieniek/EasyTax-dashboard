using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasyTax.Core.Domain.Changelog;

/// <summary>Reads the changelog file shipped with the app and picks the releases a user has not seen yet.</summary>
public static class ChangelogReader
{
	private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
	{
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
	};

	/// <summary>Parses <c>changelog.json</c>. Returns the releases newest first.</summary>
	public static IReadOnlyList<ChangelogRelease> Parse(string json)
	{
		var file = JsonSerializer.Deserialize<ChangelogFile>(json, Options);

		return [.. (file?.Releases ?? []).OrderByDescending(release => release.Version)];
	}

	/// <summary>
	/// The releases to show after an update: newer than <paramref name="lastSeen"/> and not newer than <paramref name="current"/>,
	/// newest first. Without a last seen version (first start of the version that introduced the changelog, or a new
	/// installation) only the current release is shown.
	/// </summary>
	public static IReadOnlyList<ChangelogRelease> Since(IEnumerable<ChangelogRelease> releases, Version? lastSeen, Version current)
	{
		var now = Normalize(current);
		var seen = lastSeen is null ? null : Normalize(lastSeen);

		return
		[
			.. releases
				.Where(release => seen is null ? Normalize(release.Version) == now : Normalize(release.Version) > seen && Normalize(release.Version) <= now)
				.OrderByDescending(release => release.Version),
		];
	}

	// 1.1.0 and 1.1.0.0 are the same version here (the assembly version has a fourth part, the file does not).
	private static Version Normalize(Version version) => new(version.Major, version.Minor, Math.Max(version.Build, 0));

	private sealed record ChangelogFile(IReadOnlyList<ChangelogRelease> Releases);
}
