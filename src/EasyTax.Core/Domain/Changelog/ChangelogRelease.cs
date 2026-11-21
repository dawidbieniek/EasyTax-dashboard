namespace EasyTax.Core.Domain.Changelog;

/// <summary>One group of changes of the same kind within a release.</summary>
public sealed record ChangelogSection(ChangeKind Kind, IReadOnlyList<string> Items);

/// <summary>What changed in one version of the app.</summary>
public sealed record ChangelogRelease(Version Version, DateOnly? Date, IReadOnlyList<ChangelogSection> Sections);
