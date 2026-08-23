using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SlnDependencyStudio.Wpf.Controls;

/// <summary>
/// A reusable form field component providing a consistent visual pattern for all settings
/// fields. Renders a bold title, an optional description, the input content, and an
/// optional validation error — stacked vertically in a single column.
///
/// <para><b>Convention:</b> Use FormField for every labeled field on settings pages.
/// The <c>Title</c> is the field name. <c>Description</c> provides context (optional).
/// The child element is the input control (TextBox, ToggleButton, ItemsControl, etc.).
/// Validation errors are wired via <c>BindValidation</c> in the view code-behind.</para>
/// </summary>
[ContentProperty(nameof(InputContent))]
public partial class FormField : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(FormField),
            new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(FormField),
            new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty InputContentProperty =
        DependencyProperty.Register(
            nameof(InputContent),
            typeof(object),
            typeof(FormField),
            new FrameworkPropertyMetadata(null));

    public static readonly DependencyProperty ValidationErrorProperty =
        DependencyProperty.Register(
            nameof(ValidationError),
            typeof(string),
            typeof(FormField),
            new FrameworkPropertyMetadata(null));

    /// <summary>The bold title displayed above the input.</summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Optional lighter description text displayed between the title and the input.</summary>
    public string? Description
    {
        get => (string?)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>
    /// The input control displayed below the title and description.
    /// This is set automatically by the XAML parser when a child element
    /// is placed inside the <c>FormField</c> tag.
    /// </summary>
    public object InputContent
    {
        get => GetValue(InputContentProperty);
        set => SetValue(InputContentProperty, value);
    }

    /// <summary>
    /// Optional validation error message displayed below the input.
    /// <see langword="null"/> or empty hides the error row.
    /// </summary>
    public string? ValidationError
    {
        get => (string?)GetValue(ValidationErrorProperty);
        set => SetValue(ValidationErrorProperty, value);
    }

    public FormField()
    {
        InitializeComponent();
    }
}
