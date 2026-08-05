namespace SlnDependencyStudio.Wpf.Editors;

/// <summary>
/// Constraint marker for reactive document-editor wrappers. Its only purpose is to constrain
/// <see cref="IStudioEditorFactory.CreateEditor{TEditor}"/> to editor types.
/// </summary>
public interface IStudioEditor
{
}
