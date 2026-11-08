using EasyTax.Core.Domain.Clients;

namespace EasyTax.Core.Backups;

/// <summary>What was found in a deletion log file.</summary>
/// <param name="Entries">The readable entries (one per client; if an id appears twice the earlier time is kept).</param>
/// <param name="SkippedLines">Lines without a readable client id (damaged, for example by a crash while writing). They are ignored, but a restore reports them. A line with a readable id but an unreadable time still counts.</param>
public sealed record DeletionLogContent(IReadOnlyList<DeletedClient> Entries, int SkippedLines);

/// <summary>
/// <c>deletions.log</c> in the backup folder: one line per deleted client (id and time), append-only. It is the second copy of the
/// deletion record. The database table is in every backup, but a client deleted after the last backup would otherwise come back if
/// the database were lost before the next one. A plain file, so it works on a network share like the backups do.
/// </summary>
public interface IDeletionLog
{
	/// <summary>Reads the log in the folder. A missing file is an empty log. Throws <see cref="IOException"/> when the file exists but cannot be read.</summary>
	Task<DeletionLogContent> ReadAsync(string folder, CancellationToken cancellationToken = default);

	/// <summary>Adds the entries that are not in the log yet (so calling it again is harmless). Creates the folder and the file when needed.</summary>
	Task AppendAsync(string folder, IEnumerable<DeletedClient> entries, CancellationToken cancellationToken = default);
}
