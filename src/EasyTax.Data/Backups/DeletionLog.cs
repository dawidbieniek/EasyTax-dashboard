using System.Globalization;
using System.Text;
using EasyTax.Core.Backups;
using EasyTax.Core.Domain.Clients;

namespace EasyTax.Data.Backups;

/// <summary>
/// File format, one entry per line: <c>{id};{time}</c> with the id in the usual 36-character form and the time as UTC in the
/// round-trip format, for example <c>018f3a52-...;2026-11-08T10:15:30.1234567Z</c>. Lines are only ever added.
/// </summary>
public sealed class DeletionLog : IDeletionLog
{
	public const string FileName = "deletions.log";

	private const char Separator = ';';

	public async Task<DeletionLogContent> ReadAsync(string folder, CancellationToken cancellationToken = default)
	{
		var path = Path.Combine(folder, FileName);
		if (!File.Exists(path))
			return new DeletionLogContent([], 0);

		var lines = await ReadLinesAsync(path, cancellationToken);

		var entries = new Dictionary<Guid, DeletedClient>();
		var skipped = 0;
		foreach (var line in lines)
		{
			if (string.IsNullOrWhiteSpace(line))
				continue;

			if (!TryParse(line, out var entry))
			{
				skipped++;
				continue;
			}

			// Twice the same client (a log merged from two places): keep the earlier time.
			if (!entries.TryGetValue(entry.Id, out var known) || entry.DeletedAt < known.DeletedAt)
				entries[entry.Id] = entry;
		}

		return new DeletionLogContent([.. entries.Values], skipped);
	}

	public async Task AppendAsync(string folder, IEnumerable<DeletedClient> entries, CancellationToken cancellationToken = default)
	{
		var wanted = entries.DistinctBy(e => e.Id).ToList();
		if (wanted.Count == 0)
			return;

		Directory.CreateDirectory(folder);
		var path = Path.Combine(folder, FileName);

		var existing = (await ReadAsync(folder, cancellationToken)).Entries.Select(e => e.Id).ToHashSet();
		var missing = wanted.Where(e => !existing.Contains(e.Id)).ToList();
		if (missing.Count == 0)
			return;

		var text = new StringBuilder();

		// A crash can leave the last line without its line break; start on a fresh line so the new entry is not glued to it.
		if (!EndsWithLineBreak(path))
			text.Append('\n');

		foreach (var entry in missing)
			text.Append(Format(entry)).Append('\n');

		await File.AppendAllTextAsync(path, text.ToString(), new UTF8Encoding(false), cancellationToken);
	}

	private static string Format(DeletedClient entry)
		=> string.Create(CultureInfo.InvariantCulture, $"{entry.Id:D}{Separator}{DateTime.SpecifyKind(entry.DeletedAt, DateTimeKind.Utc):O}");

	// What matters in a line is the id: a deletion whose time is unreadable must still keep the client deleted, so a line with a
	// valid id is always accepted. An unreadable time becomes "now", and if another source knows the real time the earlier one wins.
	private static bool TryParse(string line, out DeletedClient entry)
	{
		entry = null!;

		var parts = line.Trim().Split(Separator);
		if (!Guid.TryParseExact(parts[0].Trim(), "D", out var id) || id == Guid.Empty)
			return false;

		var time = DateTime.UtcNow;
		if (parts.Length >= 2 && DateTime.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
			time = parsed;

		entry = new DeletedClient { Id = id, DeletedAt = time };

		return true;
	}

	// Shared read: another copy of the app (or a backup program) may have the file open.
	private static async Task<string[]> ReadLinesAsync(string path, CancellationToken cancellationToken)
	{
		await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var reader = new StreamReader(stream, Encoding.UTF8);

		var text = await reader.ReadToEndAsync(cancellationToken);

		return text.Split('\n');
	}

	private static bool EndsWithLineBreak(string path)
	{
		if (!File.Exists(path))
			return true;

		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		if (stream.Length == 0)
			return true;

		stream.Seek(-1, SeekOrigin.End);

		return stream.ReadByte() == '\n';
	}
}
