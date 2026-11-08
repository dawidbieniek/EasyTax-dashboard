using Microsoft.Data.Sqlite;

namespace EasyTax.Data;

/// <summary>Settings for the persistence layer.</summary>
/// <param name="databasePath">Full path of the SQLCipher database file.</param>
public sealed class EasyTaxDataOptions(string databasePath)
{
	public string DatabasePath { get; } = databasePath;

	/// <summary>
	/// The key the database is encrypted with. The app sets it once at startup, before the database is first touched;
	/// opening the database without it is refused rather than creating an unencrypted file.
	/// </summary>
	public string? Password { get; set; }

	/// <summary>Connection string for the app's own key. Refuses to run without one so an unencrypted database is never created.</summary>
	internal string ConnectionString()
	{
		if (string.IsNullOrEmpty(Password))
			throw new InvalidOperationException("The database key has not been set.");

		return ConnectionString(Password, pooling: true);
	}

	internal string ConnectionString(string password, bool pooling)
		=> new SqliteConnectionStringBuilder { DataSource = DatabasePath, Password = password, Pooling = pooling }.ToString();
}
