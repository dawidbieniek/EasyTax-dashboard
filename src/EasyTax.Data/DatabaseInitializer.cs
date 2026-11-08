using EasyTax.Core.Database;
using EasyTax.Core.Domain.Banks;
using EasyTax.Core.Domain.Years;
using EasyTax.Data.Banks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyTax.Data;

// The context factory is fetched when needed, not injected: building it requires the database key, which the app only knows after the key startup.
internal sealed class DatabaseInitializer(IServiceProvider services, EasyTaxDataOptions options) : IDatabaseInitializer
{
	private const int FirstSeededYear = 2026;
	private const int SeededYearCount = 5;

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		var directory = Path.GetDirectoryName(options.DatabasePath);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		var contextFactory = services.GetRequiredService<IDbContextFactory<EasyTaxDbContext>>();
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var isNewDatabase = !(await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Any();
		await context.Database.MigrateAsync(cancellationToken);

		await SeedBanksAsync(context, cancellationToken);

		// Years are user-managed afterwards, so the defaults are only added once, when the database is created.
		if (isNewDatabase)
			await SeedYearsAsync(context, cancellationToken);
	}

	public async Task<bool> CanOpenAsync(string key, CancellationToken cancellationToken = default)
	{
		try
		{
			// Its own unpooled connection: a wrong key must not leave a connection behind in the pool.
			await using var connection = new SqliteConnection(options.ConnectionString(key, pooling: false));
			await connection.OpenAsync(cancellationToken);

			await using var command = connection.CreateCommand();
			command.CommandText = "SELECT count(*) FROM sqlite_master";
			await command.ExecuteScalarAsync(cancellationToken);

			return true;
		}
		catch (SqliteException)
		{
			return false;
		}
	}

	private static async Task SeedBanksAsync(EasyTaxDbContext context, CancellationToken cancellationToken)
	{
		var existing = await context.Banks.Select(b => b.Name).ToListAsync(cancellationToken);

		context.Banks.AddRange(BankSeed.Names.Except(existing).Select(name => new Bank { Name = name }));
		await context.SaveChangesAsync(cancellationToken);
	}

	private static async Task SeedYearsAsync(EasyTaxDbContext context, CancellationToken cancellationToken)
	{
		context.AccountingYears.AddRange(Enumerable.Range(FirstSeededYear, SeededYearCount).Select(year => new AccountingYear(year)));
		await context.SaveChangesAsync(cancellationToken);
	}
}
