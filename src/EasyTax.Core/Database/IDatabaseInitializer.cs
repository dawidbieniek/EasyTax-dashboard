namespace EasyTax.Core.Database;

/// <summary>Prepares the database (creates or migrates it, seeds lookup data) before the app starts using it.</summary>
public interface IDatabaseInitializer
{
	Task InitializeAsync(CancellationToken cancellationToken = default);

	/// <summary>True when the database file can be opened and read with this key (used to check a recovery key before it is kept).</summary>
	Task<bool> CanOpenAsync(string key, CancellationToken cancellationToken = default);
}
