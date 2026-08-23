namespace SlnDependencyStudio.Wpf.Editors;

/// <summary>
/// Factory for resolving reactive document-editor wrappers from DI.
/// </summary>
public interface IStudioEditorFactory
{
    /// <summary>Resolves the editor registered for the requested editor interface.</summary>
    /// <typeparam name="TEditor">The editor interface type to resolve.</typeparam>
    /// <returns>The resolved editor instance.</returns>
    TEditor CreateEditor<TEditor>() where TEditor : IStudioEditor;
}
