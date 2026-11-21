namespace EasyTax.App.Changelog;

/// <summary>Shows what changed in the app: the new versions after an update, or the whole history.</summary>
public interface IChangelogService
{
	/// <summary>Shows the releases newer than the last one the user saw, if any, and remembers that they were shown. Call once at startup.</summary>
	Task ShowNewAsync();

	/// <summary>Shows every release, newest first.</summary>
	Task ShowAllAsync();
}
