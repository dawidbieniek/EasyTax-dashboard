using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyTax.App.Infrastructure;
using EasyTax.App.Localization;
using EasyTax.Core.Domain.Alerts;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Emails;
using EasyTax.Core.Domain.Limits;

namespace EasyTax.App.Settings;

/// <summary>The "Szablony e-mail" tab: pick a template, edit its subject and text (saved a moment after typing stops), see a live preview.</summary>
public sealed partial class EmailTemplatesViewModel(IEmailTemplateRepository templates, ILocalizer localizer) : ObservableObject
{
	private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(800);

	private readonly Debouncer _debouncer = new(SaveDelay);
	private readonly Dictionary<string, EmailTemplate> _stored = [];

	private string? _currentKey;
	private bool _isDirty;
	private bool _isLoading;

	public IReadOnlyList<TemplateOption> Options { get; } =
	[
		.. EmailTemplateSlot.All.Select(slot => new TemplateOption(slot, localizer[$"EmailTemplate_{slot.Key.Replace('.', '_')}"])),
	];

	[ObservableProperty]
	public partial TemplateOption? SelectedOption { get; set; }

	[ObservableProperty]
	public partial string Subject { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string Body { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PreviewSubject { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PreviewBody { get; set; } = string.Empty;

	public async Task LoadAsync()
	{
		_stored.Clear();
		foreach (var (key, template) in await templates.GetAllAsync())
			_stored[key] = template;

		SelectedOption ??= Options[0];
		await ShowAsync(SelectedOption);
	}

	/// <summary>Saves a pending edit right away (when switching template or leaving the page).</summary>
	public async Task FlushAsync()
	{
		if (_isDirty && _currentKey is { } key)
			await SaveAsync(key, Subject, Body);
	}

	partial void OnSelectedOptionChanged(TemplateOption? value)
	{
		if (value is not null && value.Slot.Key != _currentKey)
			_ = SwitchAsync(value);
	}

	partial void OnSubjectChanged(string value) => OnEdited();

	partial void OnBodyChanged(string value) => OnEdited();

	[RelayCommand]
	private async Task ResetAsync()
	{
		if (_currentKey is not { } key)
			return;

		var template = await templates.ResetAsync(key);
		if (template is null)
			return;

		_stored[key] = template;
		_isDirty = false;
		Display(template);
	}

	private async Task SwitchAsync(TemplateOption option)
	{
		await FlushAsync();
		await ShowAsync(option);
	}

	private Task ShowAsync(TemplateOption option)
	{
		_currentKey = option.Slot.Key;
		_isDirty = false;

		if (_stored.TryGetValue(option.Slot.Key, out var template))
			Display(template);

		return Task.CompletedTask;
	}

	private void Display(EmailTemplate template)
	{
		_isLoading = true;
		Subject = template.Subject;
		Body = template.Body;
		_isLoading = false;

		UpdatePreview();
	}

	private void OnEdited()
	{
		if (_isLoading || _currentKey is not { } key)
			return;

		_isDirty = true;
		UpdatePreview();

		// Capture what to save now, so a fast switch to another template still saves into the right one.
		var (subject, body) = (Subject, Body);
		_ = _debouncer.ScheduleAsync(() => SaveAsync(key, subject, body));
	}

	private async Task SaveAsync(string key, string subject, string body)
	{
		await templates.SaveAsync(key, subject, body);

		if (_stored.TryGetValue(key, out var template))
		{
			template.Subject = subject;
			template.Body = body;
		}

		if (key == _currentKey && subject == Subject && body == Body)
			_isDirty = false;
	}

	private void UpdatePreview()
	{
		if (SelectedOption is not { } option)
			return;

		var alert = SampleAlert(option.Slot);
		PreviewSubject = EmailTemplateRenderer.Render(Subject, alert);
		PreviewBody = EmailTemplateRenderer.Render(Body, alert);
	}

	// A made-up client and alert, so the preview shows what the tokens turn into.
	private static ClientAlert SampleAlert(EmailTemplateSlot slot)
	{
		var client = new Client
		{
			FirstName = "Anna",
			LastName = "Nowak",
			Gender = Gender.Female,
			Nip = "5252248481",
			JoinDate = new DateOnly(2024, 1, 1),
			BankId = Guid.Empty,
			ReliefType = slot.Relief ?? ReliefType.FullZus,
		};

		var today = DateOnly.FromDateTime(DateTime.Today);
		var critical = slot.Severity == AlertSeverity.Critical;

		if (slot.Kind == AlertKind.ReliefEnding)
		{
			var days = critical ? -3 : 5;
			return new ClientAlert(client, slot.Kind, slot.Severity, today.Year, ReliefEndDate: today.AddDays(days), DaysLeft: days);
		}

		var (type, limit) = slot.Kind switch
		{
			AlertKind.VatLimit => (LimitType.Vat, LimitCalculator.VatBaseLimit),
			AlertKind.CashRegisterLimit => (LimitType.CashRegister, LimitCalculator.CashRegisterLimit),
			_ => (LimitType.ZusPlus, LimitCalculator.ZusPlusLimit),
		};
		var status = new LimitStatus(type, true, limit * (critical ? 1.04m : 0.93m), limit, new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31));

		return new ClientAlert(client, slot.Kind, slot.Severity, today.Year, Limit: status);
	}
}

public sealed record TemplateOption(EmailTemplateSlot Slot, string Title);
