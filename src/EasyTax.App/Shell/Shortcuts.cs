using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace EasyTax.App.Shell;

/// <summary>
/// Keyboard shortcuts. Every shortcut is a KeyboardAccelerator on the control it belongs to, so WinUI shows the key
/// in that control's tooltip. Keys are listed here so they can be changed in one place.
/// </summary>
public static class Shortcuts
{
	// Keys without a VirtualKey name. (The bracket keys, 219 and 221, are not matched by KeyboardAccelerator.)
	public const VirtualKey Comma = (VirtualKey)188;
	public const VirtualKey Period = (VirtualKey)190;
	public const VirtualKey Slash = (VirtualKey)191;

	/// <summary>
	/// Runs <paramref name="action"/> when the key is pressed. Plain keys are ignored while the user types into a text
	/// control, so letters still type; chords with Ctrl or Alt always run. Nothing runs under an open dialog or drop-down.
	/// </summary>
	/// <param name="owner">The control the shortcut belongs to; its tooltip shows the key.</param>
	/// <param name="whileTyping">Also run while a text control has focus (for keys like Escape).</param>
	public static void Add(UIElement owner, VirtualKey key, Action action, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None, bool whileTyping = false)
	{
		var isChord = modifiers.HasFlag(VirtualKeyModifiers.Control) || modifiers.HasFlag(VirtualKeyModifiers.Menu);
		var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };

		accelerator.Invoked += (_, args) =>
		{
			// Handled also stops the control's own default action (a button click); the action below replaces it.
			args.Handled = true;

			var root = ((FrameworkElement)args.Element).XamlRoot;
			if (root is null || HasOpenPopup(root))
				return;

			if (!isChord && !whileTyping && (IsTyping(root) || FocusManager.GetFocusedElement(root) is ComboBox))
				return;

			action();
		};

		// The built-in accelerator tooltip does not appear on buttons and pops up over whole panels elsewhere, so it is
		// switched off (before the accelerator is added) and buttons get their own tooltip instead.
		owner.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
		owner.KeyboardAccelerators.Add(accelerator);

		if (owner is ContentControl button)
			EnsureTooltip(button);
	}

	/// <summary>
	/// Moves focus after the key press has been fully processed. Focusing a text box right away would let it receive the
	/// character of the shortcut key itself.
	/// </summary>
	public static void FocusLater(Control control)
	{
		if (!control.IsEnabled)
			return;

		control.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => control.Focus(FocusState.Keyboard));
	}

	// "Name (key)", built when the tooltip opens so a changing label (accounted / not accounted) is always current.
	private static void EnsureTooltip(ContentControl owner)
	{
		if (ToolTipService.GetToolTip(owner) is not null)
			return;

		// A tooltip without content never opens, so start with the current text and refresh it each time it opens.
		var tip = new ToolTip { Content = Describe(owner) };
		tip.Opened += (_, _) => tip.Content = Describe(owner);
		ToolTipService.SetToolTip(owner, tip);
	}

	private static string Describe(ContentControl owner)
	{
		var name = AutomationProperties.GetName(owner);
		if (string.IsNullOrEmpty(name))
			name = owner.Content as string ?? string.Empty;

		var keys = string.Join(", ", owner.KeyboardAccelerators.Select(a => KeyText(a.Key, a.Modifiers)));

		return string.IsNullOrEmpty(name) ? keys : $"{name} ({keys})";
	}

	private static string KeyText(VirtualKey key, VirtualKeyModifiers modifiers)
	{
		var text = key switch
		{
			VirtualKey.Left => "←",
			VirtualKey.Right => "→",
			VirtualKey.Delete => "Del",
			VirtualKey.Escape => "Esc",
			>= VirtualKey.Number0 and <= VirtualKey.Number9 => ((int)key - (int)VirtualKey.Number0).ToString(System.Globalization.CultureInfo.InvariantCulture),
			Comma => ",",
			Period => ".",
			Slash => "/",
			_ => key.ToString(),
		};

		var prefix = string.Empty;
		if (modifiers.HasFlag(VirtualKeyModifiers.Control))
			prefix += "Ctrl+";
		if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
			prefix += "Alt+";
		if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
			prefix += "Shift+";

		return prefix + text;
	}

	/// <summary>Moves focus off a text control when Escape is pressed or the user clicks elsewhere.</summary>
	/// <param name="sink">An element that takes focus when nothing else should have it.</param>
	public static void Blur(XamlRoot root, Control sink)
	{
		if (IsTyping(root))
			sink.Focus(FocusState.Programmatic);
	}

	/// <summary>
	/// Clicking an empty area leaves focus in the text box, so give focus to <paramref name="sink"/> instead. Clicks on
	/// controls that keep focus where it is (the notes toolbar) and clicks inside pop-ups are left alone.
	/// </summary>
	public static void BlurOnClickAway(UIElement root, Control sink)
	{
		root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, e) =>
		{
			var xamlRoot = root.XamlRoot;
			if (xamlRoot is null || !IsTyping(xamlRoot) || HasOpenPopup(xamlRoot))
				return;

			var focused = FocusManager.GetFocusedElement(xamlRoot) as DependencyObject;
			for (var element = e.OriginalSource as DependencyObject; element is not null; element = VisualTreeHelper.GetParent(element))
			{
				if (element == focused || element is ButtonBase { AllowFocusOnInteraction: false })
					return;
			}

			// Let the click give focus to what was clicked first; only an empty area leaves the text box focused.
			root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				if (ReferenceEquals(FocusManager.GetFocusedElement(xamlRoot), focused))
					sink.Focus(FocusState.Pointer);
			});
		}), handledEventsToo: true);
	}

	// A dialog, flyout or drop-down is open. Tooltips are pop-ups as well but must not block shortcuts.
	private static bool HasOpenPopup(XamlRoot root)
		=> VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Any(popup => popup.Child is not ToolTip && popup.Child?.GetType().Name.Contains("ToolTip", StringComparison.Ordinal) != true);

	private static bool IsTyping(XamlRoot root) => FocusManager.GetFocusedElement(root) is TextBox or RichEditBox or PasswordBox;
}
