namespace EasyTax.Core.Domain.Months;

public enum MonthState
{
	/// <summary>The month is earlier than the client's join month, so it cannot be selected.</summary>
	BeforeJoin,

	NotAccounted,

	Accounted,
}
