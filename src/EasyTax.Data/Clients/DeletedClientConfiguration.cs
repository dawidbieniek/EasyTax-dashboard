using EasyTax.Core.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Clients;

internal sealed class DeletedClientConfiguration : IEntityTypeConfiguration<DeletedClient>
{
	public void Configure(EntityTypeBuilder<DeletedClient> builder)
	{
		builder.ToTable("DeletedClients");
		builder.HasKey(d => d.Id);
		builder.Property(d => d.Id).ValueGeneratedNever();
		builder.Property(d => d.DeletedAt).IsRequired();
	}
}
