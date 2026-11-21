using EasyTax.Core.Domain.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Emails;

internal sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
	public void Configure(EntityTypeBuilder<EmailTemplate> builder)
	{
		builder.Property(t => t.Key).HasMaxLength(64).IsRequired();
		builder.HasIndex(t => t.Key).IsUnique();
		builder.Property(t => t.Subject).HasMaxLength(256).IsRequired();
		builder.Property(t => t.Body).HasMaxLength(4000).IsRequired();
	}
}
