namespace SlnDependencyStudio.Wpf.Editors;

/// <summary>
/// Default implementation of <see cref="IStudioEditorFactory"/> that resolves editors from the
/// injected editor collection.
/// </summary>
internal sealed class StudioEditorFactory : IStudioEditorFactory
{
    private readonly IStudioEditor[] _editors;

    /// <summary>Initializes a new instance of <see cref="StudioEditorFactory"/>.</summary>
    /// <param name="editors">The registered editor instances. Each editor is exposed through the shared
    /// <see cref="IStudioEditor"/> marker in the composition root, so the collection is injected here
    /// instead of the factory service-locating from the container.</param>
    public StudioEditorFactory(IEnumerable<IStudioEditor> editors)
    {
        _editors = [.. editors];
    }

    /// <inheritdoc />
    public TEditor CreateEditor<TEditor>() where TEditor : IStudioEditor
    {
        return _editors.OfType<TEditor>().Single();
    }
}
