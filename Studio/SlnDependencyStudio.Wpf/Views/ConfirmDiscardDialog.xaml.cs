using System.Windows.Controls;

namespace SlnDependencyStudio.Wpf.Views;

/// <summary>A Material Design-themed confirmation dialog with Save, Discard, and Cancel buttons.
/// Set <see cref="Message"/> before displaying via <c>DialogHost.Show</c>.</summary>
public partial class ConfirmDiscardDialog : UserControl
{
    /// <summary>The message displayed to the user.</summary>
    public string Message
    {
        get => MessageText.Text;
        set => MessageText.Text = value;
    }

    public ConfirmDiscardDialog()
    {
        InitializeComponent();
    }
}
