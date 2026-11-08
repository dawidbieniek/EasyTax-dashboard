using EasyTax.App.Localization;
using Microsoft.Data.Sqlite;

namespace EasyTax.App.Infrastructure;

/// <summary>Turns an exception into a message a user can act on. The technical details go to the log, never to the screen.</summary>
internal static class UserFacingErrors
{
	// SQLite result codes.
	private const int Busy = 5;
	private const int Locked = 6;
	private const int ReadOnly = 8;
	private const int Corrupt = 11;
	private const int Full = 13;
	private const int NotADatabase = 26;

	public static string Describe(Exception exception, ILocalizer localizer)
	{
		for (var current = exception; current is not null; current = current.InnerException)
		{
			if (current is SqliteException sqlite)
				return localizer[DatabaseKey(sqlite.SqliteErrorCode)];

			if (current is IOException)
				return localizer["Error_Disk"];
		}

		return localizer["Error_Generic"];
	}

	private static string DatabaseKey(int code) => code switch
	{
		Busy or Locked => "Error_DbBusy",
		ReadOnly => "Error_DbReadOnly",
		Full => "Error_DiskFull",
		Corrupt or NotADatabase => "Error_DbCorrupt",
		_ => "Error_Db",
	};
}
