namespace EasyTax.App.Localization;

/// <summary>Looks up UI strings from the resource files (Strings/pl-PL/Resources.resw) for use in code.</summary>
public interface ILocalizer
{
	string this[string key] { get; }
}
