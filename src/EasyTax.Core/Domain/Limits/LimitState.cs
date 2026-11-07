namespace EasyTax.Core.Domain.Limits;

public enum LimitState
{
	Ok,

	/// <summary>At or above 90% of the limit.</summary>
	Warning,

	/// <summary>At or above the limit.</summary>
	Over,
}
