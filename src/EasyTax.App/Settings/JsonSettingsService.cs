using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasyTax.App.Settings;

public sealed class JsonSettingsService(string filePath) : ISettingsService
{
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() },
	};

	public AppSettings Current { get; private set; } = new();

	public event EventHandler? Changed;

	public async Task LoadAsync()
	{
		if (!File.Exists(filePath))
			return;

		try
		{
			await using var stream = File.OpenRead(filePath);
			Current = await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions) ?? new AppSettings();
		}
		catch (Exception ex) when (ex is JsonException or IOException)
		{
			// A corrupt or unreadable file must never stop the app from starting: fall back to defaults.
			Current = new AppSettings();
		}
	}

	public void Update(Func<AppSettings, AppSettings> change)
	{
		var updated = change(Current);
		if (updated == Current)
			return;

		Current = updated;
		Save();
		Changed?.Invoke(this, EventArgs.Empty);
	}

	private void Save()
	{
		Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
		File.WriteAllText(filePath, JsonSerializer.Serialize(Current, SerializerOptions));
	}
}
