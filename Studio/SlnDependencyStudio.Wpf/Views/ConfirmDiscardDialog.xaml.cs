using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SlnDependencyStudio.Wpf.Views;

/// <summary>
/// A confirmation dialog with Save, Discard, and Cancel buttons, styled
/// consistently with <see cref="MessageDialog"/>.
/// </summary>
public partial class ConfirmDiscardDialog : UserControl
{
    /// <summary>
    /// A DynamicResource key for the icon foreground and accent colours.
    /// Defaults to <c>"MaterialDesign.Brush.Primary"</c>.
    /// </summary>
    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(
            nameof(IconForeground),
            typeof(string),
            typeof(ConfirmDiscardDialog),
            new PropertyMetadata("MaterialDesign.Brush.Primary", OnIconForegroundChanged));

    /// <summary>
    /// A DynamicResource key for the left accent strip background.
    /// Defaults to <c>"MaterialDesign.Brush.Primary"</c>.
    /// </summary>
    public static readonly DependencyProperty AccentBrushKeyProperty =
        DependencyProperty.Register(
            nameof(AccentBrushKey),
            typeof(string),
            typeof(ConfirmDiscardDialog),
            new PropertyMetadata("MaterialDesign.Brush.Primary", OnAccentBrushKeyChanged));

    /// <summary>Initializes a new instance of <see cref="ConfirmDiscardDialog"/>.</summary>
    public ConfirmDiscardDialog()
    {
        InitializeComponent();
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

    /// <summary>Gets or sets the dialog title (e.g. "Save changes to 'ProjectName'?").</summary>
    public string Title
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    /// <summary>Gets or sets the message displayed to the user.</summary>
    public string Message
    {
        get => MessageText.Text;
        set => MessageText.Text = value;
    }

    private static void OnIconForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dialog = (ConfirmDiscardDialog)d;
        var key = (string)e.NewValue;
        dialog.IconControl.SetResourceReference(ForegroundProperty, key);
        dialog.IconBackground.SetResourceReference(Ellipse.FillProperty, key);
        dialog.AccentStrip.SetResourceReference(Border.BackgroundProperty, key);
    }

    private static void OnAccentBrushKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dialog = (ConfirmDiscardDialog)d;
        dialog.AccentStrip.SetResourceReference(Border.BackgroundProperty, (string)e.NewValue);
    }
}
