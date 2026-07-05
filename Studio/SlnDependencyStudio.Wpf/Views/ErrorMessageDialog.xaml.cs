using System.Windows.Controls;

namespace SlnDependencyStudio.Wpf.Views;

public partial class ErrorMessageDialog : UserControl
{
    public ErrorMessageDialog()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => TitleText.Text;
        set => TitleText.Text = value;
    }

    public string Message
    {
        get => MessageText.Text;
        set => MessageText.Text = value;
    }
}
