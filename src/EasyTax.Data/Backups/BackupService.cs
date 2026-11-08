using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using EasyTax.Core.Backups;
using EasyTax.Core.Domain.Clients;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Backups;

internal sealed partial class BackupService(EasyTaxDataOptions options) : IBackupService
{
	private const int ManifestFormat = 1;
	private const int BatchSize = 500;
	private const string DatabaseEntry = "easytax.db";
	private const string ManifestEntry = "manifest.json";

	private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

	// EasyTax-2026-11-08-1015.zip, but not EasyTax-prerestore-... (the safety copies).
	[GeneratedRegex(@"^EasyTax-\d{4}-\d{2}-\d{2}-\d{4}\.zip$")]
	private static partial Regex BackupFileName();

	public async Task<BackupInfo> RunAsync(string folder, string namePrefix = "EasyTax", CancellationToken cancellationToken = default)
	{
		var key = options.Password ?? throw new InvalidOperationException("The database key has not been set.");
		Directory.CreateDirectory(folder);

		var workFolder = Path.Combine(Path.GetTempPath(), "EasyTax-backup-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(workFolder);

		var createdAt = DateTimeOffset.Now;
		var finalPath = Path.Combine(folder, $"{namePrefix}-{createdAt:yyyy-MM-dd-HHmm}.zip");
		var temporaryPath = finalPath + ".tmp";

		try
		{
			var copyPath = Path.Combine(workFolder, DatabaseEntry);
			await Task.Run(() => CopyDatabase(options.DatabasePath, copyPath, key), cancellationToken);

			if (!await IsHealthyAsync(copyPath, key, cancellationToken))
				throw new InvalidDataException("The copy of the database failed the integrity check.");

			var manifest = new BackupManifest(ManifestFormat, AppVersion(), await ReadSchemaAsync(copyPath, key, cancellationToken), createdAt);
			await WriteZipAsync(temporaryPath, copyPath, manifest, cancellationToken);

			// Last step: until now there was no .zip, so an interrupted backup can never look finished.
			File.Move(temporaryPath, finalPath, overwrite: true);

			return new BackupInfo(finalPath, manifest);
		}
		finally
		{
			TryDelete(temporaryPath);
			TryDeleteFolder(workFolder);
		}
	}

	public void DeleteStaleTempFiles(string folder)
	{
		if (!Directory.Exists(folder))
			return;

		foreach (var file in Directory.EnumerateFiles(folder, "*.zip.tmp"))
			TryDelete(file);
	}

	public BackupInfo? GetLatestBackup(string folder)
	{
		if (!Directory.Exists(folder))
			return null;

		// The timestamp is in the file name, so the newest name is the newest backup; skip files that are not readable.
		var names = Directory.EnumerateFiles(folder, "EasyTax-*.zip")
			.Where(path => BackupFileName().IsMatch(Path.GetFileName(path)))
			.OrderByDescending(path => path, StringComparer.Ordinal);

		foreach (var path in names)
		{
			var manifest = TryReadManifest(path);
			if (manifest is not null)
				return new BackupInfo(path, manifest);
		}

		return null;
	}

	public Task<BackupManifest> ReadManifestAsync(string zipPath, CancellationToken cancellationToken = default)
		=> Task.Run(() => TryReadManifest(zipPath) ?? throw new InvalidDataException("Not an EasyTax backup."), cancellationToken);

	public Task<string> ExtractAsync(string zipPath, CancellationToken cancellationToken = default) => Task.Run(() =>
	{
		var folder = Path.Combine(Path.GetTempPath(), "EasyTax-restore-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(folder);

		try
		{
			using var zip = ZipFile.OpenRead(zipPath);
			var entry = zip.GetEntry(DatabaseEntry) ?? throw new InvalidDataException("The backup holds no database.");

			var target = Path.Combine(folder, DatabaseEntry);
			entry.ExtractToFile(target);

			return target;
		}
		catch
		{
			TryDeleteFolder(folder);
			throw;
		}
	}, cancellationToken);

	public void DiscardExtracted(string extractedDatabasePath)
	{
		var folder = Path.GetDirectoryName(extractedDatabasePath);
		if (!string.IsNullOrEmpty(folder))
			TryDeleteFolder(folder);
	}

	public async Task<bool> IsHealthyAsync(string databasePath, string key, CancellationToken cancellationToken = default)
	{
		try
		{
			await using var connection = Open(databasePath, key);
			await connection.OpenAsync(cancellationToken);

			await using var command = connection.CreateCommand();
			command.CommandText = "PRAGMA integrity_check";

			return (string?)await command.ExecuteScalarAsync(cancellationToken) == "ok";
		}
		catch (SqliteException)
		{
			// Wrong key and a damaged file both end up here: either way it cannot be used.
			return false;
		}
	}

	public bool IsFromNewerVersion(BackupManifest manifest)
	{
		var known = KnownMigrations();

		return manifest.Schema.Length > 0 && !known.Contains(manifest.Schema, StringComparer.Ordinal);
	}

	public async Task MigrateAsync(string databasePath, string key, CancellationToken cancellationToken = default)
	{
		var contextOptions = new DbContextOptionsBuilder<EasyTaxDbContext>().UseSqlite(EasyTaxDataOptions.ConnectionString(databasePath, key, pooling: false)).Options;

		await using var context = new EasyTaxDbContext(contextOptions);
		await context.Database.MigrateAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<DeletedClient>> ReadDeletionsAsync(string databasePath, string key, CancellationToken cancellationToken = default)
	{
		await using var context = OpenContext(databasePath, key);
		return await context.DeletedClients.AsNoTracking().ToListAsync(cancellationToken);
	}

	public async Task<int> ApplyDeletionsAsync(string databasePath, string key, IReadOnlyCollection<DeletedClient> deletions, CancellationToken cancellationToken = default)
	{
		if (deletions.Count == 0)
			return 0;

		await using var context = OpenContext(databasePath, key);
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

		var ids = deletions.Select(d => d.Id).Distinct().ToList();
		var removed = 0;

		// In batches: SQLite limits how many values one statement may carry.
		foreach (var batch in ids.Chunk(BatchSize))
		{
			var batchIds = batch.ToList();
			await context.MonthRecords.Where(m => batchIds.Contains(m.ClientId)).ExecuteDeleteAsync(cancellationToken);
			await context.ClientPhotos.Where(p => batchIds.Contains(p.ClientId)).ExecuteDeleteAsync(cancellationToken);
			removed += await context.Clients.Where(c => batchIds.Contains(c.Id)).ExecuteDeleteAsync(cancellationToken);
		}

		// Merge into this file's own record, so the next backup made from it (and a later restore) still knows.
		var known = (await context.DeletedClients.Select(d => d.Id).ToListAsync(cancellationToken)).ToHashSet();
		context.DeletedClients.AddRange(deletions.DistinctBy(d => d.Id).Where(d => !known.Contains(d.Id)));
		await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return removed;
	}

	public void ReplaceLiveDatabase(string databasePath)
	{
		// Idle connections are kept in a pool and keep the file open; drop them so the file can be replaced.
		SqliteConnection.ClearAllPools();

		var live = options.DatabasePath;
		File.Move(databasePath, live, overwrite: true);

		// Leftovers of the old database's write-ahead log must not be applied to the new file.
		TryDelete(live + "-wal");
		TryDelete(live + "-shm");
	}

	private static EasyTaxDbContext OpenContext(string databasePath, string key)
		=> new(new DbContextOptionsBuilder<EasyTaxDbContext>().UseSqlite(EasyTaxDataOptions.ConnectionString(databasePath, key, pooling: false)).Options);

	private static void CopyDatabase(string sourcePath, string destinationPath, string key)
	{
		using var source = Open(sourcePath, key);
		using var destination = Open(destinationPath, key);
		source.Open();
		destination.Open();

		// SQLite's online backup: a consistent copy even while the app keeps writing.
		source.BackupDatabase(destination);
	}

	private static SqliteConnection Open(string path, string key)
		=> new(new SqliteConnectionStringBuilder { DataSource = path, Password = key, Pooling = false }.ToString());

	private static async Task<string> ReadSchemaAsync(string databasePath, string key, CancellationToken cancellationToken)
	{
		await using var connection = Open(databasePath, key);
		await connection.OpenAsync(cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC LIMIT 1";

		return (string?)await command.ExecuteScalarAsync(cancellationToken) ?? string.Empty;
	}

	private static async Task WriteZipAsync(string zipPath, string databasePath, BackupManifest manifest, CancellationToken cancellationToken)
	{
		await using var stream = new FileStream(zipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
		using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

		// The database is already encrypted, so compressing it again would only cost time.
		zip.CreateEntryFromFile(databasePath, DatabaseEntry, CompressionLevel.NoCompression);

		var entry = zip.CreateEntry(ManifestEntry, CompressionLevel.Optimal);
		await using var entryStream = entry.Open();
		await JsonSerializer.SerializeAsync(entryStream, manifest, JsonOptions, cancellationToken);
	}

	private static BackupManifest? TryReadManifest(string zipPath)
	{
		try
		{
			using var zip = ZipFile.OpenRead(zipPath);
			if (zip.GetEntry(DatabaseEntry) is null || zip.GetEntry(ManifestEntry) is not { } entry)
				return null;

			using var stream = entry.Open();
			return JsonSerializer.Deserialize<BackupManifest>(stream, JsonOptions);
		}
		catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
		{
			return null;
		}
	}

	private static string[] KnownMigrations()
	{
		// Reading the list of migrations needs no database connection.
		var contextOptions = new DbContextOptionsBuilder<EasyTaxDbContext>().UseSqlite("Data Source=:memory:").Options;
		using var context = new EasyTaxDbContext(contextOptions);

		return [.. context.Database.GetMigrations()];
	}

	private static string AppVersion()
		=> Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";

	private static void TryDelete(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort: a leftover temporary file is harmless.
		}
	}

	private static void TryDeleteFolder(string path)
	{
		try
		{
			Directory.Delete(path, recursive: true);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// Best effort: the temp folder is cleaned by Windows eventually.
		}
	}
}
