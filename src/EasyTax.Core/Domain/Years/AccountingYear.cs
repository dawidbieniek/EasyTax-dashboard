namespace EasyTax.Core.Domain.Years;

/// <summary>A year the user works with. The list is user-managed; the app starts with 2026–2030.</summary>
public class AccountingYear(int year)
{
	public int Year { get; } = year;
}
