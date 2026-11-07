using EasyTax.Core.Domain.Months;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Months;

internal sealed class MonthRecordRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IMonthRecordRepository
{
	public async Task<IReadOnlyList<MonthRecord>> GetForYearAsync(Guid clientId, int year, CancellationToken cancellationToken = default)
	{
		var from = new DateOnly(year, 1, 1);
		var to = new DateOnly(year, 12, 31);

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.MonthRecords
			.AsNoTracking()
			.Where(m => m.ClientId == clientId && m.Month >= from && m.Month <= to)
			.OrderBy(m => m.Month)
			.ToListAsync(cancellationToken);
	}

	public async Task<IReadOnlyList<MonthRecord>> GetForMonthAsync(DateOnly month, CancellationToken cancellationToken = default)
	{
		var firstOfMonth = new DateOnly(month.Year, month.Month, 1);

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.MonthRecords
			.AsNoTracking()
			.Where(m => m.Month == firstOfMonth)
			.ToListAsync(cancellationToken);
	}

	public async Task SaveAsync(MonthRecord record, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var exists = await context.MonthRecords.AnyAsync(m => m.Id == record.Id, cancellationToken);
		context.Entry(record).State = exists ? EntityState.Modified : EntityState.Added;

		await context.SaveChangesAsync(cancellationToken);
	}
}
