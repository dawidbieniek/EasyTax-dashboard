using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using UiControls = Microsoft.UI.Xaml.Controls;

namespace EasyTax.App.Controls;

/// <summary>
/// A form row: header, an editor (the content) shown while <see cref="IsEditing"/>, the plain
/// <see cref="DisplayText"/> otherwise, and an optional <see cref="Error"/> message.
/// </summary>
[ContentProperty(Name = nameof(FieldContent))]
public sealed partial class FormField : UiControls.UserControl
{
	public static readonly DependencyProperty HeaderProperty = Register<string>(nameof(Header), string.Empty);

	public static readonly DependencyProperty FieldContentProperty = Register<object?>(nameof(FieldContent), null);

	public static readonly DependencyProperty IsEditingProperty = Register<bool>(nameof(IsEditing), true);

	public static readonly DependencyProperty DisplayTextProperty = Register<string>(nameof(DisplayText), string.Empty);

	public static readonly DependencyProperty ErrorProperty = Register<string>(nameof(Error), string.Empty);

	public FormField()
	{
		InitializeComponent();
	}

	public string Header
	{
		get => (string)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	public object? FieldContent
	{
		get => GetValue(FieldContentProperty);
		set => SetValue(FieldContentProperty, value);
	}

	public bool IsEditing
	{
		get => (bool)GetValue(IsEditingProperty);
		set => SetValue(IsEditingProperty, value);
	}

	public string DisplayText
	{
		get => (string)GetValue(DisplayTextProperty);
		set => SetValue(DisplayTextProperty, value);
	}

	public string Error
	{
		get => (string)GetValue(ErrorProperty);
		set => SetValue(ErrorProperty, value);
	}

	public static Visibility ShowWhenFalse(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

	public static Visibility ShowWhenText(string? value) => string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;

	private static DependencyProperty Register<T>(string name, T? defaultValue)
		=> DependencyProperty.Register(name, typeof(T), typeof(FormField), new PropertyMetadata(defaultValue));
}
