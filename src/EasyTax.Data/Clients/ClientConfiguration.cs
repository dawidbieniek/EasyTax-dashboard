using EasyTax.Core.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Clients;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
	public void Configure(EntityTypeBuilder<Client> builder)
	{
		builder.HasKey(c => c.Id);
		builder.Property(c => c.FirstName).HasMaxLength(64).IsRequired();
		builder.Property(c => c.LastName).HasMaxLength(64).IsRequired();
	}
}
