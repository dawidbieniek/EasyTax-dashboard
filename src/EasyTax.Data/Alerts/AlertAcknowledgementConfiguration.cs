using EasyTax.Core.Domain.Alerts;
using EasyTax.Core.Domain.Clients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EasyTax.Data.Alerts;

internal sealed class AlertAcknowledgementConfiguration : IEntityTypeConfiguration<AlertAcknowledgement>
{
	public void Configure(EntityTypeBuilder<AlertAcknowledgement> builder)
	{
		// One acknowledgement per client, kind and year; acknowledging again replaces it.
		builder.HasIndex(a => new { a.ClientId, a.Kind, a.Year }).IsUnique();

		builder.HasOne<Client>()
			.WithMany()
			.HasForeignKey(a => a.ClientId)
			.OnDelete(DeleteBehavior.Cascade);
	}
}
