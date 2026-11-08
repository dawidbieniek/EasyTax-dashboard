using EasyTax.App.Localization;
using EasyTax.App.Settings;
using EasyTax.Core.Database;
using EasyTax.Data;

namespace EasyTax.App.Security;

/// <summary>
/// Decides, before the database is touched, which key to use: the stored one, a new one (first start, shown to the user once)
/// or the recovery key (stored key missing or wrong). Rule: a new key is only ever created when there is no database file.
/// </summary>
internal static class DatabaseKeyStartup
{
	private static readonly byte[] PlainHeader = "SQLite format 3\0"u8.ToArray();

	/// <summary>Sets <see cref="EasyTaxDataOptions.Password"/>. False when the user closed a window instead of finishing, so the app should exit.</summary>
	public static async Task<bool> PrepareAsync(DatabaseKeyService keys, EasyTaxDataOptions options, IDatabaseInitializer database, ILocalizer localizer, AppTheme theme)
	{
		var databaseExists = File.Exists(options.DatabasePath);
		if (databaseExists && IsPlain(options.DatabasePath))
			throw new PlainDatabaseException();

		if (!databaseExists)
			return await FirstStartAsync(keys, options, localizer, theme);

		var stored = keys.TryLoad();
		if (stored is not null && await database.CanOpenAsync(stored))
		{
			options.Password = stored;
			return true;
		}

		return await RecoverAsync(keys, options, database, localizer, theme);
	}

	private static async Task<bool> FirstStartAsync(DatabaseKeyService keys, EasyTaxDataOptions options, ILocalizer localizer, AppTheme theme)
	{
		var key = DatabaseKeyService.Generate();

		if (!await new KeySetupWindow(key, localizer, theme).ShowAsync())
			return false;

		// Stored only now: closing the window earlier leaves nothing behind, so the next start is a first start again.
		keys.Store(key);
		options.Password = key;

		return true;
	}

	private static async Task<bool> RecoverAsync(DatabaseKeyService keys, EasyTaxDataOptions options, IDatabaseInitializer database, ILocalizer localizer, AppTheme theme)
	{
		var window = new KeyRecoveryWindow(localizer, theme, key => database.CanOpenAsync(key));
		if (!await window.ShowAsync() || window.Key is null)
			return false;

		// The key is right: wrap it again for this computer / Windows profile so the next start needs no typing.
		keys.Store(window.Key);
		options.Password = window.Key;

		return true;
	}

	private static bool IsPlain(string path)
	{
		using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

		var header = new byte[PlainHeader.Length];
		var read = stream.Read(header, 0, header.Length);

		return read == header.Length && header.AsSpan().SequenceEqual(PlainHeader);
	}
}
