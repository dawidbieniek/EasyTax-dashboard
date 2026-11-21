using EasyTax.Core.Domain.Banks;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Banks;

internal sealed class BankRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IBankRepository
{
	private static readonly StringComparer PolishOrder = StringComparer.Create(new System.Globalization.CultureInfo("pl-PL"), ignoreCase: true);

	public async Task<IReadOnlyList<Bank>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		var banks = await context.Banks
			.AsNoTracking()
			.ToListAsync(cancellationToken);

		// Polish alphabet, not character codes ("Deutsche" before "DNB", "Ikano" before "ING").
		return [.. banks.OrderBy(b => b.Name, PolishOrder)];
	}
}
