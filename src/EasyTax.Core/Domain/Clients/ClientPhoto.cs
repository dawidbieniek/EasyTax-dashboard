namespace EasyTax.Core.Domain.Clients;

/// <summary>Optional picture of a client. Kept apart from <see cref="Client"/> so lists never load the image bytes.</summary>
public class ClientPhoto
{
	public Guid ClientId { get; init; }

	public required byte[] Bytes { get; set; }

	public required string ContentType { get; set; }
}
