namespace EasyTax.Core.Domain.Years;

public interface IAccountingYearRepository
{
	/// <summary>All years, ascending.</summary>
	Task<IReadOnlyList<AccountingYear>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>Adds the year. Returns false when it already exists.</summary>
	Task<bool> AddAsync(int year, CancellationToken cancellationToken = default);

	/// <summary>True when any client has month data in that year, which blocks removing it.</summary>
	Task<bool> HasDataAsync(int year, CancellationToken cancellationToken = default);

	/// <summary>Removes the year unless it has data. Returns true when it was removed.</summary>
	Task<bool> TryRemoveAsync(int year, CancellationToken cancellationToken = default);
}
