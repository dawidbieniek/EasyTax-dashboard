using EasyTax.Core.Clients;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Clients;

internal sealed class ClientRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IClientRepository
{
	public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.Clients
			.AsNoTracking()
			.OrderBy(c => c.LastName)
			.ThenBy(c => c.FirstName)
			.ToListAsync(cancellationToken);
	}

	public async Task<int> CountAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.Clients.CountAsync(cancellationToken);
	}

	public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		context.Clients.Add(client);
		await context.SaveChangesAsync(cancellationToken);
	}
}
