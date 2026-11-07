using EasyTax.Core.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Clients;

internal sealed class ClientPhotoRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IClientPhotoRepository
{
	public async Task<ClientPhoto?> GetAsync(Guid clientId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.ClientPhotos
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.ClientId == clientId, cancellationToken);
	}

	public async Task<IReadOnlyDictionary<Guid, byte[]>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.ClientPhotos
			.AsNoTracking()
			.ToDictionaryAsync(p => p.ClientId, p => p.Bytes, cancellationToken);
	}

	public async Task SaveAsync(Guid clientId, byte[] bytes, string contentType, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var existing = await context.ClientPhotos.FirstOrDefaultAsync(p => p.ClientId == clientId, cancellationToken);
		if (existing is null)
		{
			context.ClientPhotos.Add(new ClientPhoto { ClientId = clientId, Bytes = bytes, ContentType = contentType });
		}
		else
		{
			existing.Bytes = bytes;
			existing.ContentType = contentType;
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task RemoveAsync(Guid clientId, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		await context.ClientPhotos.Where(p => p.ClientId == clientId).ExecuteDeleteAsync(cancellationToken);
	}
}
