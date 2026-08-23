namespace SlnDependencyStudio.Wpf.Editors;

/// <summary>
/// Constraint marker for reactive document-editor wrappers. It constrains
/// <see cref="IStudioEditorFactory.CreateEditor{TEditor}"/> to editor types and also serves as the
/// shared registration base that the factory resolves as a collection (each editor is explicitly
/// aliased to this marker in the composition root).
/// </summary>
public interface IStudioEditor
{
}
