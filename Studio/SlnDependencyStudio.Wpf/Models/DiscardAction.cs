namespace SlnDependencyStudio.Wpf.Models;

/// <summary>Represents the user's choice in a save-before-discard confirmation dialog.</summary>
public enum DiscardAction
{
    /// <summary>Save changes before proceeding.</summary>
    Save,

    /// <summary>Discard changes and proceed.</summary>
    Discard,

    /// <summary>Cancel the operation — do nothing.</summary>
    Cancel
}
