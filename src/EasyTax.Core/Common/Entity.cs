namespace EasyTax.Core.Common;

/// <summary>Base class for persisted domain objects.</summary>
public abstract class Entity
{
	/// <summary>
	/// Time-ordered GUID (v7). Assigned when the object is created, so it is valid before it is ever saved
	/// and the database never has to generate it.
	/// </summary>
	public Guid Id { get; init; } = Guid.CreateVersion7();
}
