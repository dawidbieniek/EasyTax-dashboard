namespace EasyTax.App.Shell;

/// <summary>One paragraph of a dialog's message.</summary>
/// <param name="Text">The paragraph.</param>
/// <param name="IsBold">Emphasize it (for example the name of the thing being deleted).</param>
public sealed record DialogText(string Text, bool IsBold = false);
