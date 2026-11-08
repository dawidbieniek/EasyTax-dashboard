using System.ComponentModel;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using EasyTax.App.Shell;
using Windows.System;
using Windows.Globalization.NumberFormatting;

namespace EasyTax.App.Dashboard;

public sealed partial class DashboardPage : Page
{
	private const string RtfPrefix = "{\\rtf";

	// True while text is being loaded into the editor, so loading it is not mistaken for typing.
	private bool _loadingNotes;

	// True while the view model's Notes is being set from the editor, so it is not loaded straight back.
	private bool _notesFromEditor;

	public DashboardPage()
	{
		ViewModel = App.GetService<DashboardViewModel>();
		DataContext = ViewModel;
		InitializeComponent();

		IncomeBox.NumberFormatter = CreateFormatter(fractionDigits: 2);
		SalesBox.NumberFormatter = CreateFormatter(fractionDigits: 0);
		CostBox.NumberFormatter = CreateFormatter(fractionDigits: 0);

		Shortcuts.Add(IncomeBox, VirtualKey.I, () => Shortcuts.FocusLater(IncomeBox));
		Shortcuts.Add(SalesBox, VirtualKey.S, () => Shortcuts.FocusLater(SalesBox));
		Shortcuts.Add(CostBox, VirtualKey.C, () => Shortcuts.FocusLater(CostBox));
		Shortcuts.Add(NotesEditor, VirtualKey.N, () => Shortcuts.FocusLater(NotesEditor));

		LoadNotes(ViewModel.Notes);
		ViewModel.PropertyChanged += OnViewModelPropertyChanged;
		Unloaded += (_, _) => ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
	}

	public DashboardViewModel ViewModel { get; }

	// Checklist buttons come from a template, so each one gets its shortcut when it is created.
	private void OnChecklistLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is not ToggleButton { Tag: ChecklistKey key } button)
			return;

		if (button.KeyboardAccelerators.Count > 0)
			return;

		Shortcuts.Add(button, KeyFor(key), () => ToggleChecklist(key));
	}

	private async void ToggleChecklist(ChecklistKey key)
	{
		var isMonthItem = key is ChecklistKey.JpkV7 or ChecklistKey.VatUe;
		if (isMonthItem && !ViewModel.IsMonthEditable)
			return;

		await ViewModel.ToggleChecklistCommand.ExecuteAsync(key);
	}

	private static VirtualKey KeyFor(ChecklistKey key) => key switch
	{
		ChecklistKey.VatR => VirtualKey.R,
		ChecklistKey.Ceidg1 => VirtualKey.E,
		ChecklistKey.Zus => VirtualKey.U,
		ChecklistKey.Invoiced => VirtualKey.F,
		ChecklistKey.ZusDra => VirtualKey.D,
		ChecklistKey.Tax => VirtualKey.T,
		ChecklistKey.JpkV7 => VirtualKey.J,
		_ => VirtualKey.V,
	};

	private async void OnChecklistClick(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleButton { Tag: ChecklistKey key })
			await ViewModel.ToggleChecklistCommand.ExecuteAsync(key);
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		// Another client's notes were shown: put them into the editor.
		if (e.PropertyName == nameof(DashboardViewModel.Notes) && !_notesFromEditor)
			LoadNotes(ViewModel.Notes);
	}

	/// <summary>Notes are stored as RTF text in the client's Notes column. Plain text from before formatting existed is shown as it is.</summary>
	private void LoadNotes(string notes)
	{
		_loadingNotes = true;

		var isRtf = notes.StartsWith(RtfPrefix, StringComparison.Ordinal);
		NotesEditor.Document.SetText(isRtf ? TextSetOptions.FormatRtf : TextSetOptions.None, isRtf ? notes : notes.TrimEnd('\r', '\n'));

		// Any change event caused by SetText is already queued; this runs after it.
		DispatcherQueue.TryEnqueue(() => _loadingNotes = false);
	}

	private void OnNotesTextChanged(object sender, RoutedEventArgs e)
	{
		if (_loadingNotes)
			return;

		NotesEditor.Document.GetText(TextGetOptions.None, out var plainText);

		// An empty editor is stored as an empty string, not as an empty RTF document.
		var stored = string.Empty;
		if (!string.IsNullOrWhiteSpace(plainText))
			NotesEditor.Document.GetText(TextGetOptions.FormatRtf, out stored);

		_notesFromEditor = true;
		ViewModel.Notes = stored;
		_notesFromEditor = false;
	}

	private void OnBoldClick(object sender, RoutedEventArgs e) => ChangeCharacterFormat(format => format.Bold = FormatEffect.Toggle);

	private void OnItalicClick(object sender, RoutedEventArgs e) => ChangeCharacterFormat(format => format.Italic = FormatEffect.Toggle);

	private void OnUnderlineClick(object sender, RoutedEventArgs e)
		=> ChangeCharacterFormat(format => format.Underline = format.Underline == UnderlineType.None ? UnderlineType.Single : UnderlineType.None);

	private void OnBulletsClick(object sender, RoutedEventArgs e)
	{
		var selection = NotesEditor.Document.Selection;
		var paragraph = selection.ParagraphFormat;
		paragraph.ListType = paragraph.ListType == MarkerType.Bullet ? MarkerType.None : MarkerType.Bullet;
		selection.ParagraphFormat = paragraph;
	}

	private void ChangeCharacterFormat(Action<ITextCharacterFormat> change)
	{
		var selection = NotesEditor.Document.Selection;
		var format = selection.CharacterFormat;
		change(format);
		selection.CharacterFormat = format;
	}

	/// <summary>NumberBox shows its value through a formatter; this one fixes the number of decimals (amounts: 2, counts: 0).</summary>
	private static DecimalFormatter CreateFormatter(int fractionDigits) => new()
	{
		FractionDigits = fractionDigits,
		IntegerDigits = 1,
		NumberRounder = new IncrementNumberRounder
		{
			Increment = fractionDigits == 0 ? 1 : 0.01,
			RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp,
		},
	};
}
