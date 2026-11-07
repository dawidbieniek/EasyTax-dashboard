namespace EasyTax.Core.Common;

public static class DateOnlyExtensions
{
	extension(DateOnly date)
	{
		public DateOnly FirstDayOfMonth => new(date.Year, date.Month, 1);

		public DateOnly FirstDayOfYear => new(date.Year, 1, 1);

		public DateOnly LastDayOfYear => new(date.Year, 12, 31);

		public int DaysInYear => new DateOnly(date.Year + 1, 1, 1).DayNumber - date.FirstDayOfYear.DayNumber;

		/// <summary>Days from this date to the end of its year, counting the date itself.</summary>
		public int DaysUntilEndOfYear => new DateOnly(date.Year + 1, 1, 1).DayNumber - date.DayNumber;
	}
}
