namespace EasyTax.Core.Domain.Clients;

public interface IClientPhotoRepository
{
	Task<ClientPhoto?> GetAsync(Guid clientId, CancellationToken cancellationToken = default);

	/// <summary>The pictures of all clients that have one, by client id (for lists).</summary>
	Task<IReadOnlyDictionary<Guid, byte[]>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>Stores the picture, replacing any existing one.</summary>
	Task SaveAsync(Guid clientId, byte[] bytes, string contentType, CancellationToken cancellationToken = default);

	Task RemoveAsync(Guid clientId, CancellationToken cancellationToken = default);
}
