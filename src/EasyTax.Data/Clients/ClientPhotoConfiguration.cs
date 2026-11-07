using EasyTax.Core.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Clients;

internal sealed class ClientPhotoConfiguration : IEntityTypeConfiguration<ClientPhoto>
{
	public void Configure(EntityTypeBuilder<ClientPhoto> builder)
	{
		builder.HasKey(p => p.ClientId);
		builder.Property(p => p.ClientId).ValueGeneratedNever();
		builder.Property(p => p.Bytes).IsRequired();
		builder.Property(p => p.ContentType).HasMaxLength(64).IsRequired();
	}
}
