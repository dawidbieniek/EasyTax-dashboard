using EasyTax.Core.Domain.Alerts;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Alerts;

internal sealed class AlertAcknowledgementRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IAlertAcknowledgementRepository
{
	public async Task<IReadOnlyList<AlertAcknowledgement>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.AlertAcknowledgements.AsNoTracking().ToListAsync(cancellationToken);
	}

	public async Task AcknowledgeAsync(ClientAlert alert, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		var existing = await context.AlertAcknowledgements
			.FirstOrDefaultAsync(a => a.ClientId == alert.Client.Id && a.Kind == alert.Kind && a.Year == alert.Year, cancellationToken);

		if (existing is null)
		{
			context.AlertAcknowledgements.Add(new AlertAcknowledgement { ClientId = alert.Client.Id, Kind = alert.Kind, Year = alert.Year, Level = alert.Severity });
		}
		else
		{
			existing.Level = alert.Severity;
			existing.AcknowledgedAt = DateTime.UtcNow;
		}

		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task RemoveAsync(ClientAlert alert, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		await context.AlertAcknowledgements
			.Where(a => a.ClientId == alert.Client.Id && a.Kind == alert.Kind && a.Year == alert.Year)
			.ExecuteDeleteAsync(cancellationToken);
	}
}
