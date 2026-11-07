using EasyTax.Core.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Clients;

internal sealed class ClientRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IClientRepository
{
	public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.Clients
			.AsNoTracking()
			.Include(c => c.Bank)
			.OrderBy(c => c.LastName)
			.ThenBy(c => c.FirstName)
			.ToListAsync(cancellationToken);
	}

	public async Task<Client?> GetAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.Clients
			.AsNoTracking()
			.Include(c => c.Bank)
			.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
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

	public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		context.Entry(client).State = EntityState.Modified;
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		await context.Clients.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken);
	}
}
