namespace EasyTax.App.Security;

/// <summary>The database file is an old unencrypted one. There is no migration path (see PLAN.md, E.5), so the app refuses to open it.</summary>
internal sealed class PlainDatabaseException : Exception
{
}
