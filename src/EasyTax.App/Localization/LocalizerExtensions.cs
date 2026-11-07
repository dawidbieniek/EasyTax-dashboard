namespace EasyTax.App.Localization;

public static class LocalizerExtensions
{
	extension(ILocalizer localizer)
	{
		/// <summary>Display name of an enum value, looked up as <c>EnumName_Value</c> (for example <c>TaxType_Scale</c>).</summary>
		public string Label<TEnum>(TEnum value)
			where TEnum : struct, Enum
			=> localizer[$"{typeof(TEnum).Name}_{value}"];

		/// <summary>All values of an enum with their localized names, in declaration order.</summary>
		public IReadOnlyList<EnumOption<TEnum>> Options<TEnum>()
			where TEnum : struct, Enum
			=> [.. Enum.GetValues<TEnum>().Select(value => new EnumOption<TEnum>(value, localizer.Label(value)))];
	}
}
