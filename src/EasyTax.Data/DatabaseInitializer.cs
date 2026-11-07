using EasyTax.Core.Database;
using EasyTax.Core.Domain.Banks;
using EasyTax.Core.Domain.Years;
using EasyTax.Data.Banks;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data;

internal sealed class DatabaseInitializer(IDbContextFactory<EasyTaxDbContext> contextFactory, EasyTaxDataOptions options) : IDatabaseInitializer
{
	private const int FirstSeededYear = 2026;
	private const int SeededYearCount = 5;

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		var directory = Path.GetDirectoryName(options.DatabasePath);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var isNewDatabase = !(await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Any();
		await context.Database.MigrateAsync(cancellationToken);

		await SeedBanksAsync(context, cancellationToken);

		// Years are user-managed afterwards, so the defaults are only added once, when the database is created.
		if (isNewDatabase)
			await SeedYearsAsync(context, cancellationToken);
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
