namespace EasyTax.Core.Domain.Clients;

/// <summary>
/// Remembers that a client was deleted, so restoring an old backup cannot bring them back. Holds no personal data:
/// only the id the client had and when they were deleted.
/// </summary>
public sealed class DeletedClient
{
	/// <summary>The id the deleted client had.</summary>
	public Guid Id { get; init; }

	/// <summary>When the client was deleted (UTC).</summary>
	public DateTime DeletedAt { get; init; }

	/// <summary>
	/// Combines deletion records from several places (a backup's own table, the live database, the log file) into one entry per
	/// client. When the sources disagree about the time, the earliest wins.
	/// </summary>
	public static IReadOnlyList<DeletedClient> Merge(params IEnumerable<DeletedClient>[] sources)
		=> [.. sources.SelectMany(s => s).GroupBy(d => d.Id).Select(g => g.OrderBy(d => d.DeletedAt).First())];
}
