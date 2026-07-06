using AllOverIt.Assertion;
using ReactiveUI;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Features.RecentProjects;

namespace SlnDependencyStudio.Wpf.Features.Project.Stores;

/// <summary>
/// Singleton store that is the observable source of truth for the currently open
/// dependency project document. Owns all editor wrappers and derives global dirty state.
/// </summary>
internal sealed class ProjectDocumentStore : ReactiveObject, IProjectDocumentStore
{
    private readonly IDependencyProjectService _projectService;
    private readonly IRecentProjectsService _recentProjects;
    private readonly ProjectMetadataEditor _metadataEditor;
    private readonly ObservableAsPropertyHelper<bool> _isDirty;
    private DependencyProjectDocument? _document;
    private string? _currentFilePath;
    private bool _hasDocument;

    /// <inheritdoc />
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set => this.RaiseAndSetIfChanged(ref _currentFilePath, value);
    }

    /// <inheritdoc />
    public IProjectMetadataEditor MetadataEditor => _metadataEditor;

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <inheritdoc />
    public bool HasDocument
    {
        get => _hasDocument;
        set => this.RaiseAndSetIfChanged(ref _hasDocument, value);
    }

    /// <summary>Initializes a new instance of the store.</summary>
    /// <param name="projectService">The project serialization service.</param>
    /// <param name="recentProjects">The recent projects service for MRU tracking.</param>
    public ProjectDocumentStore(IDependencyProjectService projectService, IRecentProjectsService recentProjects)
    {
        _projectService = projectService.WhenNotNull();
        _recentProjects = recentProjects.WhenNotNull();

        _metadataEditor = new ProjectMetadataEditor();

        // Global dirty state is derived from all editor wrappers. When future wrappers are added,
        // combine their IsDirty values here (e.g., using CombineLatest or additional WhenAnyValue parameters).
        _isDirty = _metadataEditor
            .WhenAnyValue(editor => editor.IsDirty)
            .ToProperty(this, nameof(IsDirty));
    }

    /// <inheritdoc />
    public async Task OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        _document = await _projectService.OpenAsync(filePath, cancellationToken);

        HasDocument = true;
        CurrentFilePath = filePath;

        _metadataEditor.SetOriginalValues(_document.Metadata);

        _recentProjects.Add(filePath);
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

        _recentProjects.Add(filePath);

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
