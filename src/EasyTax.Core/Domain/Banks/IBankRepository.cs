namespace EasyTax.Core.Domain.Banks;

public interface IBankRepository
{
	Task<IReadOnlyList<Bank>> GetAllAsync(CancellationToken cancellationToken = default);
}
