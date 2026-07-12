using AllOverIt.Assertion;
using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Features.Solution;
using System.IO;
using System.Reactive.Linq;

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
    private readonly SolutionOptionsEditor _solutionOptionsEditor;
    private readonly ExportOptionsEditor _exportOptionsEditor;
    private readonly DiagramOptionsEditor _diagramOptionsEditor;
    private readonly PreGenerationConfigEditor _preGenerationEditor;
    private readonly ObservableAsPropertyHelper<bool> _isDirty;
    private DependencyProjectDocument? _document;
    private string? _currentFilePath;
    private bool _hasDocument;
    private bool _isTransitioning;

    /// <inheritdoc />
    public string? DocumentFilePath
    {
        get => _currentFilePath;
        set => this.RaiseAndSetIfChanged(ref _currentFilePath, value);
    }

    /// <inheritdoc />
    public string DocumentDirectory => Path.GetDirectoryName(DocumentFilePath) ?? string.Empty;

    /// <inheritdoc />
    public IProjectMetadataEditor MetadataEditor => _metadataEditor;

    /// <inheritdoc />
    public ISolutionOptionsEditor SolutionOptionsEditor => _solutionOptionsEditor;

    /// <inheritdoc />
    public IExportOptionsEditor ExportOptionsEditor => _exportOptionsEditor;

    /// <inheritdoc />
    public IDiagramOptionsEditor DiagramOptionsEditor => _diagramOptionsEditor;

    /// <inheritdoc />
    public IPreGenerationConfigEditor PreGenerationEditor => _preGenerationEditor;

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <inheritdoc />
    public bool HasDocument
    {
        get => _hasDocument;
        set => this.RaiseAndSetIfChanged(ref _hasDocument, value);
    }

    /// <inheritdoc />
    public bool IsTransitioning
    {
        get => _isTransitioning;
        private set => this.RaiseAndSetIfChanged(ref _isTransitioning, value);
    }

    /// <summary>Initializes a new instance of the store.</summary>
    /// <param name="projectService">The project serialization service.</param>
    /// <param name="recentProjects">The recent projects service for MRU tracking.</param>
    public ProjectDocumentStore(IDependencyProjectService projectService, IRecentProjectsService recentProjects)
    {
        _projectService = projectService.WhenNotNull();
        _recentProjects = recentProjects.WhenNotNull();

        _metadataEditor = new ProjectMetadataEditor();
        _solutionOptionsEditor = new SolutionOptionsEditor();
        _exportOptionsEditor = new ExportOptionsEditor();
        _diagramOptionsEditor = new DiagramOptionsEditor();
        _preGenerationEditor = new PreGenerationConfigEditor();

        // Global dirty state is derived from all editor wrappers.
        _isDirty = Observable
            .CombineLatest(
                _metadataEditor.WhenAnyValue(editor => editor.IsDirty),
                _solutionOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
                _exportOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
                _diagramOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
                _preGenerationEditor.WhenAnyValue(editor => editor.IsDirty),
                (metadata, solution, export, diagrams, preGen) => metadata || solution || export || diagrams || preGen)
            .ToProperty(this, nameof(IsDirty));
    }

    /// <inheritdoc />
    public async Task OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        IsTransitioning = true;

        _document = await _projectService.OpenAsync(filePath, cancellationToken);

        HasDocument = true;
        DocumentFilePath = filePath;

        _metadataEditor.SetOriginalValues(_document.Metadata);
        _solutionOptionsEditor.SetOriginalValues(_document.DiagramGenerator.Solution);
        _exportOptionsEditor.SetOriginalValues(_document.DiagramGenerator.Export);
        _diagramOptionsEditor.SetOriginalValues(_document.DiagramGenerator.Diagram);
        _preGenerationEditor.SetOriginalValues(_document.PreGeneration);

        _recentProjects.Add(filePath);

        IsTransitioning = false;
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        Throw<InvalidOperationException>.WhenNull(_document, "No project is loaded");
        Throw<InvalidOperationException>.WhenNull(DocumentFilePath, "No document file path");

        FlushAllEditors();

        await _projectService.SaveAsync(_document, DocumentFilePath, cancellationToken);

        MarkAllEditorsClean();
    }

    /// <inheritdoc />
    public async Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        Throw<InvalidOperationException>.WhenNull(_document, "No project is loaded");
        filePath.WhenNotNull();

        FlushAllEditors();

        await _projectService.SaveAsync(_document, filePath, cancellationToken);

        DocumentFilePath = filePath;

        _recentProjects.Add(filePath);

        MarkAllEditorsClean();
    }

    /// <inheritdoc />
    public void Close()
    {
        IsTransitioning = true;

        _document = null;
        HasDocument = false;
        DocumentFilePath = null;

        // Reset editors to empty defaults so IsDirty returns to false.
        _metadataEditor.SetOriginalValues(new DependencyProjectMetadata());
        _solutionOptionsEditor.SetOriginalValues(new GeneratorSolutionOptions());
        _exportOptionsEditor.SetOriginalValues(new GeneratorExportOptions());
        _diagramOptionsEditor.SetOriginalValues(new GeneratorDiagramOptions());
        _preGenerationEditor.SetOriginalValues(new PreGenerationConfig());

        IsTransitioning = false;
    }

    /// <summary>Flushes all editor wrappers to the underlying document.</summary>
    private void FlushAllEditors()
    {
        _metadataEditor.FlushTo(_document!.Metadata);
        _solutionOptionsEditor.FlushTo(_document!.DiagramGenerator.Solution);
        _exportOptionsEditor.FlushTo(_document!.DiagramGenerator.Export);
        _diagramOptionsEditor.FlushTo(_document!.DiagramGenerator.Diagram);
        _preGenerationEditor.FlushTo(_document!.PreGeneration);
    }

    /// <summary>Marks all editor wrappers as clean after a successful save.</summary>
    private void MarkAllEditorsClean()
    {
        _metadataEditor.SetOriginalValues(_document!.Metadata);
        _solutionOptionsEditor.SetOriginalValues(_document!.DiagramGenerator.Solution);
        _exportOptionsEditor.SetOriginalValues(_document!.DiagramGenerator.Export);
        _diagramOptionsEditor.SetOriginalValues(_document!.DiagramGenerator.Diagram);
        _preGenerationEditor.SetOriginalValues(_document!.PreGeneration);
    }

    /// <inheritdoc />
    public DependencyGeneratorConfig BuildGeneratorConfig()
    {
        Throw<InvalidOperationException>.WhenNull(_document, "No project is loaded");

        FlushAllEditors();

        var config = _document.DiagramGenerator;

        // Resolve relative paths against the document directory so the generator
        // receives absolute paths regardless of the current working directory.
        if (DocumentFilePath is not null)
        {
            var docDir = Path.GetDirectoryName(DocumentFilePath)!;

            config.Solution.SolutionPath = PathUtils.ResolveAsAbsolutePath(config.Solution.SolutionPath, docDir);
            config.Export.RootPath = PathUtils.ResolveAsAbsolutePath(config.Export.RootPath, docDir);
        }

        return config;
    }
}
