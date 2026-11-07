namespace EasyTax.Core.Database;

/// <summary>Prepares the database (creates or migrates it, seeds lookup data) before the app starts using it.</summary>
public interface IDatabaseInitializer
{
	Task InitializeAsync(CancellationToken cancellationToken = default);
}
