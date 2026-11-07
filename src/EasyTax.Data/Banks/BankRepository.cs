using EasyTax.Core.Domain.Banks;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Banks;

internal sealed class BankRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IBankRepository
{
	public async Task<IReadOnlyList<Bank>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.Banks
			.AsNoTracking()
			.OrderBy(b => b.Name)
			.ToListAsync(cancellationToken);
	}
}
