namespace EasyTax.Core.Domain.Clients;

public enum TaxType
{
	/// <summary>Skala podatkowa</summary>
	Scale,

	/// <summary>Ryczałt</summary>
	LumpSum,

	/// <summary>Podatek liniowy</summary>
	Linear,
}
