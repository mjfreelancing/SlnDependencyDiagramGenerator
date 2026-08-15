namespace SlnDependencyStudio.Wpf.Enumerations;

/// <summary>Represents how document-relative paths are handled when a project is saved to a different folder.</summary>
public enum SaveAsRelativePathAction
{
    /// <summary>Rewrite each relative path to its current absolute form so it keeps pointing at the same target.</summary>
    ConvertToAbsolute,

    /// <summary>Re-express each relative path against the new document folder so it keeps pointing at the same target.</summary>
    RebaseRelative,

    /// <summary>Cancel the operation — do not save.</summary>
    Cancel
}
