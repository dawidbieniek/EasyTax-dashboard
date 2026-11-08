namespace EasyTax.App.Shell;

/// <summary>One entry of the app bar's client search list: the client's id and name, plus the photo to show in the avatar.</summary>
public sealed record ClientSuggestion(Guid Id, string FullName, byte[]? Photo);
