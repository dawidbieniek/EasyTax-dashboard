namespace EasyTax.Core.Domain.Clients;

public interface IClientRepository
{
	/// <summary>All clients (including suspended), ordered by last then first name, with their bank loaded.</summary>
	Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>A single client with its bank loaded, or null.</summary>
	Task<Client?> GetAsync(Guid id, CancellationToken cancellationToken = default);

	Task<int> CountAsync(CancellationToken cancellationToken = default);

	Task AddAsync(Client client, CancellationToken cancellationToken = default);

	Task UpdateAsync(Client client, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes the client together with its month records and photo and, in the same transaction, records the deletion
	/// (see <see cref="DeletedClient"/>). Returns the record, or null when there was no such client.
	/// </summary>
	Task<DeletedClient?> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

	/// <summary>Every deletion recorded in this database.</summary>
	Task<IReadOnlyList<DeletedClient>> GetDeletionsAsync(CancellationToken cancellationToken = default);
}
