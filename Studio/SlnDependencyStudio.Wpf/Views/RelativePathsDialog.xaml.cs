using SlnDependencyStudio.Wpf.Utils;

namespace SlnDependencyStudio.Wpf.Views;

/// <summary>
/// A modal dialog offering the user a choice for handling document-relative paths when saving a
/// project to a different folder: convert to absolute, re-base relative, or cancel.
/// </summary>
public partial class RelativePathsDialog : DialogBase
{
    /// <summary>Initializes a new instance of <see cref="RelativePathsDialog"/>.</summary>
    public RelativePathsDialog()
    {
        InitializeComponent();

        DialogIcon = IconControl;
        DialogIconBackground = IconBackground;
        DialogAccentStrip = AccentStrip;
    }

    /// <summary>Gets or sets the dialog title.</summary>
    public string Title
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    /// <summary>Gets or sets the message body.</summary>
    public string Message
    {
        get => MessageText.Text;
        set => MessageText.Text = value;
    }

    /// <summary>Gets or sets the affected relative paths displayed in the dialog.</summary>
    public IReadOnlyList<RelativePathField> RelativePaths
    {
        get => (IReadOnlyList<RelativePathField>?)RelativePathsList.ItemsSource ?? Array.Empty<RelativePathField>();
        set => RelativePathsList.ItemsSource = value;
    }
}
