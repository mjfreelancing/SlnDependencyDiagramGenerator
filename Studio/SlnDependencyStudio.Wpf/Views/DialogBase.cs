using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace SlnDependencyStudio.Wpf.Views;

/// <summary>
/// Base class for modal dialogs that share the Card + accent strip + tinted-circle-icon
/// visual pattern. Provides <see cref="IconForeground"/> and <see cref="AccentBrushKey"/>
/// dependency properties with cascading <c>SetResourceReference</c> callbacks.
/// </summary>
public abstract class DialogBase : UserControl
{
    /// <summary>
    /// A DynamicResource key for the icon foreground, icon-background ellipse,
    /// and accent strip. Defaults to <c>"MaterialDesign.Brush.Primary"</c>.
    /// </summary>
    public static readonly DependencyProperty IconForegroundProperty =
        DependencyProperty.Register(
            nameof(IconForeground),
            typeof(string),
            typeof(DialogBase),
            new PropertyMetadata("MaterialDesign.Brush.Primary", OnIconForegroundChanged));

    /// <summary>
    /// A DynamicResource key for the left accent strip background.
    /// Defaults to <c>"MaterialDesign.Brush.Primary"</c>.
    /// </summary>
    public static readonly DependencyProperty AccentBrushKeyProperty =
        DependencyProperty.Register(
            nameof(AccentBrushKey),
            typeof(string),
            typeof(DialogBase),
            new PropertyMetadata("MaterialDesign.Brush.Primary", OnAccentBrushKeyChanged));

    /// <summary>
    /// The PackIcon in the dialog header. Set by each subclass in its constructor
    /// after <c>InitializeComponent</c> from the XAML <c>x:Name="IconControl"</c> field.
    /// </summary>
    protected PackIcon DialogIcon { get; set; } = null!;

    /// <summary>
    /// The tinted background ellipse behind the icon. Set by each subclass in its
    /// constructor after <c>InitializeComponent</c> from the XAML <c>x:Name="IconBackground"</c> field.
    /// </summary>
    protected Ellipse DialogIconBackground { get; set; } = null!;

    /// <summary>
    /// The 5px accent strip on the left edge. Set by each subclass in its
    /// constructor after <c>InitializeComponent</c> from the XAML <c>x:Name="AccentStrip"</c> field.
    /// </summary>
    protected Border DialogAccentStrip { get; set; } = null!;

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

    /// <summary>
    /// Cascades the brush key to the icon, background ellipse, and accent strip
    /// via <see cref="FrameworkElement.SetResourceReference"/> so they respond to
    /// runtime theme changes.
    /// </summary>
    protected static void OnIconForegroundChanged(DependencyObject dependency, DependencyPropertyChangedEventArgs evtArgs)
    {
        var dialog = (DialogBase)dependency;
        var key = (string)evtArgs.NewValue;

        dialog.DialogIcon.SetResourceReference(ForegroundProperty, key);
        dialog.DialogIconBackground.SetResourceReference(Ellipse.FillProperty, key);
        dialog.DialogAccentStrip.SetResourceReference(Border.BackgroundProperty, key);
    }

    /// <summary>
    /// Sets the accent strip background independently of the icon colour.
    /// </summary>
    protected static void OnAccentBrushKeyChanged(DependencyObject dependency, DependencyPropertyChangedEventArgs evtArgs)
    {
        var dialog = (DialogBase)dependency;
        dialog.DialogAccentStrip.SetResourceReference(Border.BackgroundProperty, (string)evtArgs.NewValue);
    }
}
