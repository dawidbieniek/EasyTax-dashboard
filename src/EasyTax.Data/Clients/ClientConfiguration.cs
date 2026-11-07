using EasyTax.Core.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Clients;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
	public void Configure(EntityTypeBuilder<Client> builder)
	{
		builder.Property(c => c.FirstName).HasMaxLength(64).IsRequired();
		builder.Property(c => c.LastName).HasMaxLength(64).IsRequired();
		builder.Property(c => c.Nip).HasMaxLength(10).IsRequired();
		builder.Property(c => c.PhoneNumber).HasMaxLength(32);
		builder.Property(c => c.Email).HasMaxLength(256);
		builder.Property(c => c.PlanName).HasMaxLength(64);

		builder.HasIndex(c => new { c.LastName, c.FirstName });

		// A bank in use cannot be deleted.
		builder.HasOne(c => c.Bank)
			.WithMany()
			.HasForeignKey(c => c.BankId)
			.OnDelete(DeleteBehavior.Restrict);

		// Deleting a client deletes its photo and month records.
		builder.HasOne(c => c.Photo)
			.WithOne()
			.HasForeignKey<ClientPhoto>(p => p.ClientId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(c => c.Months)
			.WithOne(m => m.Client)
			.HasForeignKey(m => m.ClientId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
