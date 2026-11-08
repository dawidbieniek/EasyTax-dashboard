using EasyTax.Core.Domain.Clients;

namespace EasyTax.Core.Backups;

/// <summary>What a backup says about itself (stored as manifest.json inside the zip; contains no client data).</summary>
/// <param name="Format">Layout of the zip, so a future version can still read old backups.</param>
/// <param name="AppVersion">Version of the app that made the backup.</param>
/// <param name="Schema">Id of the last applied database migration.</param>
/// <param name="CreatedAt">When the backup was made.</param>
public sealed record BackupManifest(int Format, string AppVersion, string Schema, DateTimeOffset CreatedAt);

/// <summary>A finished backup file.</summary>
public sealed record BackupInfo(string Path, BackupManifest Manifest);

/// <summary>Makes and restores backups of the encrypted database. Backups are zips holding an encrypted copy of the database (same key) and a manifest.</summary>
public interface IBackupService
{
	/// <summary>
	/// Copies the live database into <c>EasyTax-yyyy-MM-dd-HHmm.zip</c> in the folder. The file is written under a temporary
	/// name and renamed last, so a <c>.zip</c> in the folder is always complete.
	/// </summary>
	/// <param name="folder">Destination folder (a network share works too).</param>
	/// <param name="namePrefix">Start of the file name; the safety copy made before a restore uses another one so it never counts as the latest backup.</param>
	Task<BackupInfo> RunAsync(string folder, string namePrefix = "EasyTax", CancellationToken cancellationToken = default);

	/// <summary>Removes half-written backups (<c>.tmp</c>) left behind when the app was closed during a backup.</summary>
	void DeleteStaleTempFiles(string folder);

	/// <summary>The newest readable backup in the folder, or null. The folder is the source of truth, nothing is remembered elsewhere.</summary>
	BackupInfo? GetLatestBackup(string folder);

	/// <summary>Reads the manifest of a backup file. Throws <see cref="InvalidDataException"/> when the file is not an EasyTax backup.</summary>
	Task<BackupManifest> ReadManifestAsync(string zipPath, CancellationToken cancellationToken = default);

	/// <summary>Unpacks the database of a backup into a new temporary folder and returns its path. Call <see cref="DiscardExtracted"/> when done.</summary>
	Task<string> ExtractAsync(string zipPath, CancellationToken cancellationToken = default);

	/// <summary>Deletes what <see cref="ExtractAsync"/> created.</summary>
	void DiscardExtracted(string extractedDatabasePath);

	/// <summary>True when the file opens with the key and passes SQLite's integrity check.</summary>
	Task<bool> IsHealthyAsync(string databasePath, string key, CancellationToken cancellationToken = default);

	/// <summary>True when the backup was made by a newer version of the app than this one (its schema is unknown here).</summary>
	bool IsFromNewerVersion(BackupManifest manifest);

	/// <summary>Brings an older database file up to the current schema.</summary>
	Task MigrateAsync(string databasePath, string key, CancellationToken cancellationToken = default);

	/// <summary>The deletions recorded in a (migrated) database file.</summary>
	Task<IReadOnlyList<DeletedClient>> ReadDeletionsAsync(string databasePath, string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes the given clients (with their month records and photos) from a database file and merges the deletions into its own
	/// record. Returns how many clients were actually still in the file. Used on a restored copy so deleted clients stay deleted.
	/// </summary>
	Task<int> ApplyDeletionsAsync(string databasePath, string key, IReadOnlyCollection<DeletedClient> deletions, CancellationToken cancellationToken = default);

	/// <summary>Replaces the live database with the file. Close every use of the database first; the app must restart afterwards.</summary>
	void ReplaceLiveDatabase(string databasePath);
}
