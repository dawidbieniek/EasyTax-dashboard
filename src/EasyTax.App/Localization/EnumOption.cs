namespace EasyTax.App.Localization;

/// <summary>An enum value with its localized name, for combo boxes.</summary>
public sealed record EnumOption<TEnum>(TEnum Value, string Label)
	where TEnum : struct, Enum;
