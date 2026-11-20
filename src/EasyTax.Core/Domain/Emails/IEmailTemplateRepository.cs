namespace EasyTax.Core.Domain.Emails;

public interface IEmailTemplateRepository
{
	/// <summary>All templates, keyed by <see cref="EmailTemplate.Key"/>. Missing ones are added with their defaults at startup.</summary>
	Task<IReadOnlyDictionary<string, EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>The template of a slot, or null when there is none (should not happen after startup seeding).</summary>
	Task<EmailTemplate?> GetAsync(EmailTemplateSlot slot, CancellationToken cancellationToken = default);

	Task SaveAsync(string key, string subject, string body, CancellationToken cancellationToken = default);

	/// <summary>Puts the shipped default text back and returns it.</summary>
	Task<EmailTemplate?> ResetAsync(string key, CancellationToken cancellationToken = default);
}
