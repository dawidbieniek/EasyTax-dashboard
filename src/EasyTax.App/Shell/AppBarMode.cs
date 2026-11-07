namespace EasyTax.App.Shell;

/// <summary>How much of the shared app bar a page wants to show.</summary>
public enum AppBarMode
{
	Hidden,

	/// <summary>Client picker only.</summary>
	ClientOnly,

	/// <summary>Client picker, year, months and the accounted toggle.</summary>
	Full,
}
