using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SlnDependencyStudio.Wpf.Views;

/// <summary>
/// A general-purpose modal dialog for displaying messages with a title,
/// body text, and a configurable Material Design icon.
/// </summary>
public partial class MessageDialog : UserControl
{
    /// <summary>The Material Design icon kind displayed in the dialog header.</summary>
    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(
            nameof(IconKind),
            typeof(PackIconKind),
            typeof(MessageDialog),
            new PropertyMetadata(PackIconKind.InformationOutline, OnIconKindChanged));

    /// <summary>
    /// A DynamicResource key for the icon foreground and accent colours.
    /// Also tints the icon background ellipse. Defaults to <c>"MaterialDesign.Brush.Primary"</c>.
    /// </summary>
    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(
            nameof(IconForeground),
            typeof(string),
            typeof(MessageDialog),
            new PropertyMetadata("MaterialDesign.Brush.Primary", OnIconForegroundChanged));

    /// <summary>
    /// A DynamicResource key for the left accent strip background.
    /// Defaults to <c>"MaterialDesign.Brush.Primary"</c>.
    /// </summary>
    public static readonly DependencyProperty AccentBrushKeyProperty =
        DependencyProperty.Register(
            nameof(AccentBrushKey),
            typeof(string),
            typeof(MessageDialog),
            new PropertyMetadata("MaterialDesign.Brush.Primary", OnAccentBrushKeyChanged));

    /// <summary>Initializes a new instance of <see cref="MessageDialog"/>.</summary>
    public MessageDialog()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the PackIcon kind for the dialog header.</summary>
    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    /// <summary>Gets or sets a DynamicResource key for the icon and accent colours.</summary>
    public string IconForeground
    {
        get => (string)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }

    /// <summary>Gets or sets a DynamicResource key for the left accent strip colour.</summary>
    public string AccentBrushKey
    {
        get => (string)GetValue(AccentBrushKeyProperty);
        set => SetValue(AccentBrushKeyProperty, value);
    }

    /// <summary>Gets or sets the dialog title.</summary>
    public string Title
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    /// <summary>Gets or sets the dialog message body.</summary>
    public string Message
    {
        get => MessageText.Text;
        set => MessageText.Text = value;
    }

    private static void OnIconKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dialog = (MessageDialog)d;
        dialog.IconControl.Kind = (PackIconKind)e.NewValue;
    }

    private static void OnIconForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dialog = (MessageDialog)d;
        var key = (string)e.NewValue;
        dialog.IconControl.SetResourceReference(ForegroundProperty, key);
        dialog.IconBackground.SetResourceReference(Ellipse.FillProperty, key);
        dialog.AccentStrip.SetResourceReference(Border.BackgroundProperty, key);
    }

    private static void OnAccentBrushKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dialog = (MessageDialog)d;
        dialog.AccentStrip.SetResourceReference(Border.BackgroundProperty, (string)e.NewValue);
    }
}
