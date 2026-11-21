namespace EasyTax.App.Settings;

public interface ISettingsService
{
	AppSettings Current { get; }

	/// <summary>Raised after <see cref="Current"/> changed.</summary>
	event EventHandler? Changed;

	/// <summary>True when there was no settings file at startup: a new installation (or one whose settings were deleted).</summary>
	bool IsFirstRun { get; }

	Task LoadAsync();

	/// <summary>Applies the change, persists it and raises <see cref="Changed"/>.</summary>
	void Update(Func<AppSettings, AppSettings> change);
}
