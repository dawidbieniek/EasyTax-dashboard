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

	public async Task<DeletedClient?> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		// One transaction: the client and the note that it was deleted either both happen or neither does.
		await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

		await context.MonthRecords.Where(m => m.ClientId == id).ExecuteDeleteAsync(cancellationToken);
		await context.ClientPhotos.Where(p => p.ClientId == id).ExecuteDeleteAsync(cancellationToken);
		var removed = await context.Clients.Where(c => c.Id == id).ExecuteDeleteAsync(cancellationToken);
		if (removed == 0)
			return null;

		var deleted = new DeletedClient { Id = id, DeletedAt = DateTime.UtcNow };
		context.DeletedClients.Add(deleted);
		await context.SaveChangesAsync(cancellationToken);

		await transaction.CommitAsync(cancellationToken);

		return deleted;
	}

	public async Task<IReadOnlyList<DeletedClient>> GetDeletionsAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.DeletedClients.AsNoTracking().OrderBy(d => d.DeletedAt).ToListAsync(cancellationToken);
	}

}
