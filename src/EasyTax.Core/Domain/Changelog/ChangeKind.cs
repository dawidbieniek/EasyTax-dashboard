namespace EasyTax.Core.Domain.Changelog;

/// <summary>The kind of change a changelog section lists. The JSON file uses the camelCase name (<c>features</c>, <c>fixes</c>).</summary>
public enum ChangeKind
{
	Features,
	Improvements,
	Fixes,
}
