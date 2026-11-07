namespace EasyTax.Data;

/// <summary>Settings for the persistence layer.</summary>
/// <param name="DatabasePath">Full path of the SQLite database file.</param>
public sealed record EasyTaxDataOptions(string DatabasePath);
