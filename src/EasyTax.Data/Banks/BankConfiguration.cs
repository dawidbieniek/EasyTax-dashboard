using EasyTax.Core.Domain.Banks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Banks;

internal sealed class BankConfiguration : IEntityTypeConfiguration<Bank>
{
	public void Configure(EntityTypeBuilder<Bank> builder)
	{
		builder.Property(b => b.Name).HasMaxLength(128).IsRequired();
		builder.HasIndex(b => b.Name).IsUnique();
	}
}
