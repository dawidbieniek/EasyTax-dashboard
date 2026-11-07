using EasyTax.Core.Domain.Years;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Years;

internal sealed class AccountingYearConfiguration : IEntityTypeConfiguration<AccountingYear>
{
	public void Configure(EntityTypeBuilder<AccountingYear> builder)
	{
		builder.HasKey(y => y.Year);
		builder.Property(y => y.Year).ValueGeneratedNever();
	}
}
