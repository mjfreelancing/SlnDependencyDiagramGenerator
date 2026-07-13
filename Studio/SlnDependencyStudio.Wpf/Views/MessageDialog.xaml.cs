using MaterialDesignThemes.Wpf;
using System.Windows;

namespace SlnDependencyStudio.Wpf.Views;

/// <summary>
/// A general-purpose modal dialog for displaying messages with a title,
/// body text, and a configurable Material Design icon.
/// </summary>
public partial class MessageDialog : DialogBase
{
    /// <summary>The Material Design icon kind displayed in the dialog header.</summary>
    public static readonly DependencyProperty IconKindProperty =
        DependencyProperty.Register(
            nameof(IconKind),
            typeof(PackIconKind),
            typeof(MessageDialog),
            new PropertyMetadata(PackIconKind.InformationOutline, OnIconKindChanged));

    /// <summary>Initializes a new instance of <see cref="MessageDialog"/>.</summary>
    public MessageDialog()
    {
        InitializeComponent();

        DialogIcon = IconControl;
        DialogIconBackground = IconBackground;
        DialogAccentStrip = AccentStrip;
    }

    /// <summary>Gets or sets the PackIcon kind for the dialog header.</summary>
    public PackIconKind IconKind
    {
        get => (PackIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
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
        dialog.DialogIcon.Kind = (PackIconKind)e.NewValue;
    }
}
