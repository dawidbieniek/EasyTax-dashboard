using EasyTax.Core.Common;

namespace EasyTax.Core.Clients;

/// <summary>
/// A client of the accounting office.
/// Placeholder shape: only the fields needed to prove the architecture end to end.
/// The full model is defined in PLAN.md.
/// </summary>
public class Client : Entity
{
	public required string FirstName { get; set; }

	public required string LastName { get; set; }

	public string FullName => $"{FirstName} {LastName}";
}
