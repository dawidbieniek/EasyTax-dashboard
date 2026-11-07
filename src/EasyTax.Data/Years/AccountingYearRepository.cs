using EasyTax.Core.Domain.Years;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Years;

internal sealed class AccountingYearRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IAccountingYearRepository
{
	public async Task<IReadOnlyList<AccountingYear>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.AccountingYears
			.AsNoTracking()
			.OrderBy(y => y.Year)
			.ToListAsync(cancellationToken);
	}

	public async Task<bool> AddAsync(int year, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		if (await context.AccountingYears.AnyAsync(y => y.Year == year, cancellationToken))
			return false;

		context.AccountingYears.Add(new AccountingYear(year));
		await context.SaveChangesAsync(cancellationToken);

		return true;
	}

	public async Task<bool> HasDataAsync(int year, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await HasDataAsync(context, year, cancellationToken);
	}

	public async Task<bool> TryRemoveAsync(int year, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		if (await HasDataAsync(context, year, cancellationToken))
			return false;

		var removed = await context.AccountingYears.Where(y => y.Year == year).ExecuteDeleteAsync(cancellationToken);

		return removed > 0;
	}

	private static Task<bool> HasDataAsync(EasyTaxDbContext context, int year, CancellationToken cancellationToken)
	{
		var from = new DateOnly(year, 1, 1);
		var to = new DateOnly(year, 12, 31);

		return context.MonthRecords.AnyAsync(m => m.Month >= from && m.Month <= to, cancellationToken);
	}
}
