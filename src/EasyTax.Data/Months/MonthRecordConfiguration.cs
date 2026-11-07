using EasyTax.Core.Domain.Months;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Months;

internal sealed class MonthRecordConfiguration : IEntityTypeConfiguration<MonthRecord>
{
	public void Configure(EntityTypeBuilder<MonthRecord> builder)
	{
		// One record per client and month.
		builder.HasIndex(m => new { m.ClientId, m.Month }).IsUnique();
	}
}
