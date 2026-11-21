namespace EasyTax.Core.Domain.Alerts;

public interface IAlertAcknowledgementRepository
{
	/// <summary>Every acknowledgement (a small table: at most a few per client and year).</summary>
	Task<IReadOnlyList<AlertAcknowledgement>> GetAllAsync(CancellationToken cancellationToken = default);

	/// <summary>Acknowledges the alert at its current severity, replacing an earlier acknowledgement of the same client, kind and year.</summary>
	Task AcknowledgeAsync(ClientAlert alert, CancellationToken cancellationToken = default);

	/// <summary>Takes the acknowledgement back, so the alert shows again.</summary>
	Task RemoveAsync(ClientAlert alert, CancellationToken cancellationToken = default);
}
