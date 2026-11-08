using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Localization;
using EasyTax.App.Shell;
using EasyTax.Core.Domain.Years;

namespace EasyTax.App.Settings;

public sealed partial class SettingsViewModel : ViewModelBase
{
	private const int FirstAllowedYear = 2000;
	private const int LastAllowedYear = 2100;

	private readonly ISettingsService _settings;
	private readonly IAccountingYearRepository _yearRepository;
	private readonly IClientContext _context;
	private readonly IToastService _toasts;
	private readonly ILocalizer _localizer;

	public SettingsViewModel(ISettingsService settings, ILocalizer localizer, IAccountingYearRepository yearRepository, IClientContext context, IToastService toasts)
	{
		_settings = settings;
		_yearRepository = yearRepository;
		_context = context;
		_toasts = toasts;
		_localizer = localizer;

		ThemeOptions =
		[
			new(AppTheme.System, localizer["Theme_System"]),
			new(AppTheme.Light, localizer["Theme_Light"]),
			new(AppTheme.Dark, localizer["Theme_Dark"]),
		];
		SelectedTheme = ThemeOptions.First(o => o.Value == settings.Current.Theme);
		NewYear = DateTime.Today.Year;

		AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
	}

	public IReadOnlyList<ThemeOption> ThemeOptions { get; }

	public string AppVersion { get; }

	/// <summary>The years the app offers, ascending. A year with data cannot be removed.</summary>
	public ObservableCollection<YearItem> Years { get; } = [];

	[ObservableProperty]
	public partial ThemeOption SelectedTheme { get; set; }

	/// <summary>Value of the "new year" box (NaN while it is empty).</summary>
	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(AddYearCommand))]
	public partial double NewYear { get; set; }

	partial void OnSelectedThemeChanged(ThemeOption value)
		=> _settings.Update(s => s with { Theme = value.Value });

	public override Task OnNavigatedToAsync(object? parameter) => LoadYearsAsync();

	private bool CanAddYear() => !double.IsNaN(NewYear) && NewYear is >= FirstAllowedYear and <= LastAllowedYear && Years.All(y => y.Year != (int)NewYear);

	[RelayCommand(CanExecute = nameof(CanAddYear))]
	private async Task AddYearAsync()
	{
		var year = (int)NewYear;
		if (!await _yearRepository.AddAsync(year))
			return;

		await ReloadYearsEverywhereAsync();
		_toasts.Success(string.Format(System.Globalization.CultureInfo.CurrentCulture, _localizer["Toast_YearAdded"], year));
	}

	[RelayCommand]
	private async Task RemoveYearAsync(int year)
	{
		if (!await _yearRepository.TryRemoveAsync(year))
			return;

		await ReloadYearsEverywhereAsync();
		_toasts.Success(string.Format(System.Globalization.CultureInfo.CurrentCulture, _localizer["Toast_YearRemoved"], year));
	}

	// The app bar and dashboard read the years from the shared context, so it has to reload too.
	private async Task ReloadYearsEverywhereAsync()
	{
		await _context.ReloadAsync();
		await LoadYearsAsync();
	}

	private async Task LoadYearsAsync()
	{
		var years = await _yearRepository.GetAllAsync();

		var items = new List<YearItem>();
		foreach (var year in years)
			items.Add(new YearItem(year.Year, await _yearRepository.HasDataAsync(year.Year)));

		Years.Clear();
		foreach (var item in items)
			Years.Add(item);

		AddYearCommand.NotifyCanExecuteChanged();
	}
}

public sealed record ThemeOption(AppTheme Value, string Label);

public sealed record YearItem(int Year, bool HasData)
{
	public bool CanRemove => !HasData;
}
