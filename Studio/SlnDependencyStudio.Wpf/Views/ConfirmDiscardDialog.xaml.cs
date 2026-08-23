namespace SlnDependencyStudio.Wpf.Views;

/// <summary>
/// A confirmation dialog with Save, Discard, and Cancel buttons, styled
/// consistently with <see cref="MessageDialog"/>.
/// </summary>
public partial class ConfirmDiscardDialog : DialogBase
{
    /// <summary>Initializes a new instance of <see cref="ConfirmDiscardDialog"/>.</summary>
    public ConfirmDiscardDialog()
    {
        InitializeComponent();

        DialogIcon = IconControl;
        DialogIconBackground = IconBackground;
        DialogAccentStrip = AccentStrip;
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
}
