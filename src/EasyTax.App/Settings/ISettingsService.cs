namespace EasyTax.App.Settings;

public interface ISettingsService
{
	AppSettings Current { get; }

	/// <summary>Raised after <see cref="Current"/> changed.</summary>
	event EventHandler? Changed;

	Task LoadAsync();

	/// <summary>Applies the change, persists it and raises <see cref="Changed"/>.</summary>
	void Update(Func<AppSettings, AppSettings> change);
}
