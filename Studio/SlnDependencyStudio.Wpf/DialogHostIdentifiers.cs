namespace SlnDependencyStudio.Wpf;

/// <summary>Identifiers used to target specific dialog hosts when showing dialogs.</summary>
public static class DialogHostIdentifiers
{
    /// <summary>The identifier of the application's main dialog host — the value passed to
    /// <c>DialogHost.Show(content, DialogHostIdentifiers.MainDialogHost)</c>.</summary>
    public const string MainDialogHost = "MainDialogHost";
}
