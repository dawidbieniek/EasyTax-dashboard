namespace EasyTax.Core.Clients;

public interface IClientRepository
{
	Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken cancellationToken = default);

	Task<int> CountAsync(CancellationToken cancellationToken = default);

	Task AddAsync(Client client, CancellationToken cancellationToken = default);
}
