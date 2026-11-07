namespace EasyTax.Core.Domain.Months;

public interface IMonthRecordRepository
{
	/// <summary>All records of one client in one calendar year (at most twelve), ordered by month.</summary>
	Task<IReadOnlyList<MonthRecord>> GetForYearAsync(Guid clientId, int year, CancellationToken cancellationToken = default);

	/// <summary>Inserts the record, or updates it when it already exists.</summary>
	Task SaveAsync(MonthRecord record, CancellationToken cancellationToken = default);
}
