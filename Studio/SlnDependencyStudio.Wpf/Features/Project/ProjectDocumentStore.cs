using AllOverIt.Assertion;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Shared.Config;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>
/// Singleton store that is the observable source of truth for the currently open
/// dependency project document. Owns all editor wrappers and derives global dirty state.
/// </summary>
internal sealed class ProjectDocumentStore : ReactiveObject, IProjectDocumentStore
{
    private readonly IDependencyProjectService _projectService;
    private readonly ProjectMetadataEditor _metadataEditor;
    private DependencyProjectDocument? _document;

    /// <inheritdoc />
    [Reactive]
    public string? CurrentFilePath { get; set; }

    /// <inheritdoc />
    public IProjectMetadataEditor MetadataEditor => _metadataEditor;

    /// <inheritdoc />
    [ObservableAsProperty]
    public bool IsDirty { get; }

    /// <inheritdoc />
    [Reactive]
    public bool HasDocument { get; set; }

    /// <summary>Initializes a new instance of the store.</summary>
    /// <param name="projectService">The project serialization service.</param>
    public ProjectDocumentStore(IDependencyProjectService projectService)
    {
        _projectService = projectService.WhenNotNull();

        _metadataEditor = new ProjectMetadataEditor();

        // Global dirty state is derived from all editor wrappers. When future wrappers are added,
        // combine their IsDirty values here (e.g., using CombineLatest or additional WhenAnyValue parameters).
        _metadataEditor
            .WhenAnyValue(editor => editor.IsDirty)
            .ToPropertyEx(this, vm => vm.IsDirty);
    }

    /// <inheritdoc />
    public async Task OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        _document = await _projectService.OpenAsync(filePath, cancellationToken);

        HasDocument = true;
        CurrentFilePath = filePath;

        _metadataEditor.SetOriginalValues(_document.Metadata);
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        Throw<InvalidOperationException>.WhenNull(_document, "No project is loaded");
        Throw<InvalidOperationException>.WhenNull(CurrentFilePath, "No current file path");

        FlushAllEditors();

        await _projectService.SaveAsync(_document, CurrentFilePath, cancellationToken);

        MarkAllEditorsClean();
    }

    /// <inheritdoc />
    public async Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Throw<InvalidOperationException>.WhenNull(_document, "No project is loaded");
        filePath.WhenNotNull();

        FlushAllEditors();

        await _projectService.SaveAsync(_document, filePath, cancellationToken);

        CurrentFilePath = filePath;

        MarkAllEditorsClean();
    }

    /// <inheritdoc />
    public void Close()
    {
        _document = null;
        HasDocument = false;
        CurrentFilePath = null;

        // Reset editors to empty defaults so IsDirty returns to false.
        _metadataEditor.SetOriginalValues(new DependencyProjectMetadata());
    }

    /// <summary>Flushes all editor wrappers to the underlying document.</summary>
    private void FlushAllEditors()
    {
        _metadataEditor.FlushTo(_document!.Metadata);
    }

    /// <summary>Marks all editor wrappers as clean after a successful save.</summary>
    private void MarkAllEditorsClean()
    {
        _metadataEditor.SetOriginalValues(_document!.Metadata);
    }
}
