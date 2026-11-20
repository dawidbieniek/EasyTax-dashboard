using EasyTax.Core.Domain.Emails;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data.Emails;

internal sealed class EmailTemplateRepository(IDbContextFactory<EasyTaxDbContext> contextFactory) : IEmailTemplateRepository
{
	public async Task<IReadOnlyDictionary<string, EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.EmailTemplates.AsNoTracking().ToDictionaryAsync(t => t.Key, cancellationToken);
	}

	public async Task<EmailTemplate?> GetAsync(EmailTemplateSlot slot, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		return await context.EmailTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Key == slot.Key, cancellationToken);
	}

	public async Task SaveAsync(string key, string subject, string body, CancellationToken cancellationToken = default)
	{
		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
		await context.EmailTemplates
			.Where(t => t.Key == key)
			.ExecuteUpdateAsync(set => set.SetProperty(t => t.Subject, subject).SetProperty(t => t.Body, body), cancellationToken);
	}

	public async Task<EmailTemplate?> ResetAsync(string key, CancellationToken cancellationToken = default)
	{
		var slot = EmailTemplateSlot.All.FirstOrDefault(s => s.Key == key);
		if (slot is null)
			return null;

		var (subject, body) = EmailTemplateDefaults.For(slot);
		await SaveAsync(key, subject, body, cancellationToken);

		return await GetAsync(slot, cancellationToken);
	}
}
