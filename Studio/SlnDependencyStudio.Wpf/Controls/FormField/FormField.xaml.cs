using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SlnDependencyStudio.Wpf.Controls;

/// <summary>
/// A reusable form field component that renders a label on the left and arbitrary content
/// (typically an input control) on the right, separated by a horizontal divider line.
/// Uses <c>[ContentProperty("InputContent")]</c> to avoid the inherited <c>Content</c> property
/// conflict that occurs with <c>ContentControl</c>-based approaches.
/// </summary>
[ContentProperty(nameof(InputContent))]
public partial class FormField : UserControl
{
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(FormField),
            new FrameworkPropertyMetadata(string.Empty));

    public static readonly DependencyProperty LabelVerticalAlignmentProperty =
        DependencyProperty.Register(
            nameof(LabelVerticalAlignment),
            typeof(VerticalAlignment),
            typeof(FormField),
            new FrameworkPropertyMetadata(VerticalAlignment.Center));

    public static readonly DependencyProperty InputContentProperty =
        DependencyProperty.Register(
            nameof(InputContent),
            typeof(object),
            typeof(FormField),
            new FrameworkPropertyMetadata(null));

    /// <summary>The label text displayed on the left side of the field.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// How the label is vertically aligned relative to the content.
    /// Use <see cref="VerticalAlignment.Center"/> (default) for single-line inputs,
    /// and <see cref="VerticalAlignment.Top"/> for multi-line inputs.
    /// </summary>
    public VerticalAlignment LabelVerticalAlignment
    {
        get => (VerticalAlignment)GetValue(LabelVerticalAlignmentProperty);
        set => SetValue(LabelVerticalAlignmentProperty, value);
    }

    /// <summary>
    /// The input control displayed on the right side of the field.
    /// This is set automatically by the XAML parser when a child element
    /// is placed inside the <c>FormField</c> tag.
    /// </summary>
    public object InputContent
    {
        get => GetValue(InputContentProperty);
        set => SetValue(InputContentProperty, value);
    }

    public FormField()
    {
        InitializeComponent();
    }
}
