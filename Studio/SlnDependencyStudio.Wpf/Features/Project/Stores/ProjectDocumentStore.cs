using AllOverIt.Assertion;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Editors;
using SlnDependencyStudio.Wpf.Enumerations;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.PreGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Features.Solution;
using SlnDependencyStudio.Wpf.Utils;
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
    private readonly IRecentProjectsStore _recentProjects;
    private readonly IStudioEditorFactory _editorFactory;
    private readonly ILogger<ProjectDocumentStore> _logger;
    private readonly IProjectMetadataEditor _metadataEditor;
    private readonly ISolutionOptionsEditor _solutionOptionsEditor;
    private readonly IExportOptionsEditor _exportOptionsEditor;
    private readonly IDiagramOptionsEditor _diagramOptionsEditor;
    private readonly IPreGenerationConfigEditor _preGenerationEditor;
    private readonly IRestoreSolutionEditor _restoreSolutionEditor;
    private readonly IPostGenerationConfigEditor _postGenerationEditor;
    private readonly ObservableAsPropertyHelper<bool> _isDirty;
    private DependencyProjectDocument? _document;
    private string? _currentFilePath;
    private bool _hasDocument;
    private int _documentEpoch;
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
    public IRestoreSolutionEditor RestoreSolutionEditor => _restoreSolutionEditor;

    /// <inheritdoc />
    public IPostGenerationConfigEditor PostGenerationEditor => _postGenerationEditor;

    /// <inheritdoc />
    public bool IsDirty => _isDirty.Value;

    /// <inheritdoc />
    public bool HasDocument
    {
        get => _hasDocument;
        set => this.RaiseAndSetIfChanged(ref _hasDocument, value);
    }

    /// <inheritdoc />
    public int DocumentEpoch
    {
        get => _documentEpoch;
        private set => this.RaiseAndSetIfChanged(ref _documentEpoch, value);
    }

    /// <inheritdoc />
    public bool IsTransitioning
    {
        get => _isTransitioning;
        private set => this.RaiseAndSetIfChanged(ref _isTransitioning, value);
    }

    /// <summary>Initializes a new instance of the store.</summary>
    /// <param name="projectService">The project serialization service.</param>
    /// <param name="recentProjects">The recent projects store for MRU tracking.</param>
    /// <param name="editorFactory">The factory used to resolve document editor wrappers.</param>
    /// <param name="logger">The logger instance.</param>
    public ProjectDocumentStore(IDependencyProjectService projectService, IRecentProjectsStore recentProjects,
        IStudioEditorFactory editorFactory, ILogger<ProjectDocumentStore> logger)
    {
        _projectService = projectService;
        _recentProjects = recentProjects;
        _editorFactory = editorFactory;
        _logger = logger;

        _metadataEditor = _editorFactory.CreateEditor<IProjectMetadataEditor>();
        _solutionOptionsEditor = _editorFactory.CreateEditor<ISolutionOptionsEditor>();
        _exportOptionsEditor = _editorFactory.CreateEditor<IExportOptionsEditor>();
        _diagramOptionsEditor = _editorFactory.CreateEditor<IDiagramOptionsEditor>();
        _preGenerationEditor = _editorFactory.CreateEditor<IPreGenerationConfigEditor>();
        _restoreSolutionEditor = _editorFactory.CreateEditor<IRestoreSolutionEditor>();
        _postGenerationEditor = _editorFactory.CreateEditor<IPostGenerationConfigEditor>();

        // Global dirty state is derived from all editor wrappers.
        _isDirty = Observable
            .CombineLatest(
                _metadataEditor.WhenAnyValue(editor => editor.IsDirty),
                _solutionOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
                _exportOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
                _diagramOptionsEditor.WhenAnyValue(editor => editor.IsDirty),
                _preGenerationEditor.WhenAnyValue(editor => editor.IsDirty),
                _restoreSolutionEditor.WhenAnyValue(editor => editor.IsDirty),
                _postGenerationEditor.WhenAnyValue(editor => editor.IsDirty),
                (metadata, solution, export, diagrams, preGen, restoreSolution, postGen) =>
                    metadata || solution || export || diagrams || preGen || restoreSolution || postGen)
            .ToProperty(this, nameof(IsDirty));
    }

    /// <inheritdoc />
    public async Task OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        filePath.WhenNotNull();

        _logger.LogInformation("Opening project: {FilePath}", filePath);

        IsTransitioning = true;

        _document = await _projectService.OpenAsync(filePath, cancellationToken);

        HasDocument = true;
        DocumentFilePath = filePath;
        DocumentEpoch++;

        _metadataEditor.SetOriginalValues(_document.Metadata);
        _solutionOptionsEditor.SetOriginalValues(_document.DiagramGenerator.Solution);
        _exportOptionsEditor.SetOriginalValues(_document.DiagramGenerator.Export);
        _diagramOptionsEditor.SetOriginalValues(_document.DiagramGenerator.Diagram);
        _preGenerationEditor.SetOriginalValues(_document.PreGeneration);
        _restoreSolutionEditor.SetOriginalValues(_document.RestoreSolution);
        _postGenerationEditor.SetOriginalValues(_document.PostGeneration);

        _logger.LogInformation("Project opened: {FilePath}", filePath);

        _recentProjects.Add(filePath);

        IsTransitioning = false;
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        Throw<InvalidOperationException>.WhenNull(_document, "No project is loaded");
        Throw<InvalidOperationException>.WhenNull(DocumentFilePath, "No document file path");

        _logger.LogInformation("Saving project: {FilePath}", DocumentFilePath);

        FlushAllEditors();

        await _projectService.SaveAsync(_document, DocumentFilePath, cancellationToken);

        MarkAllEditorsClean();
    }

    /// <inheritdoc />
    public Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default)
        => SaveAsAsync(filePath, SaveAsRelativePathAction.Cancel, cancellationToken);

    /// <inheritdoc />
    public async Task SaveAsAsync(string filePath, SaveAsRelativePathAction rebaseAction, CancellationToken cancellationToken = default)
    {
        Throw<InvalidOperationException>.WhenNull(_document, "No project is loaded");
        filePath.WhenNotNull();

        _logger.LogInformation("Saving project as: {FilePath}", filePath);

        if (rebaseAction != SaveAsRelativePathAction.Cancel)
        {
            RebaseRelativePaths(Path.GetDirectoryName(filePath) ?? string.Empty, rebaseAction);
        }

        FlushAllEditors();

        await _projectService.SaveAsync(_document, filePath, cancellationToken);

        DocumentFilePath = filePath;

        _recentProjects.Add(filePath);

        MarkAllEditorsClean();
    }

    /// <inheritdoc />
    public IReadOnlyList<RelativePathField> GetRelativePathFields()
    {
        var fields = new List<RelativePathField>();

        RelativePathRebaser.AddIfRelative(fields, "Solution path", _solutionOptionsEditor.SolutionPath.Value);
        RelativePathRebaser.AddIfRelative(fields, "Export root", _exportOptionsEditor.RootPath.Value);
        RelativePathRebaser.AddIfRelative(fields, "Pre-generation working directory", _preGenerationEditor.WorkingDirectory.Value);
        RelativePathRebaser.AddIfRelative(fields, "Post-generation working directory", _postGenerationEditor.WorkingDirectory.Value);

        return fields;
    }

    private void RebaseRelativePaths(string newDirectory, SaveAsRelativePathAction action)
    {
        var oldDirectory = DocumentDirectory;

        if (string.IsNullOrEmpty(oldDirectory))
        {
            return;
        }

        _solutionOptionsEditor.SolutionPath.Value = RelativePathRebaser.Rewrite(_solutionOptionsEditor.SolutionPath.Value, oldDirectory, newDirectory, action);
        _exportOptionsEditor.RootPath.Value = RelativePathRebaser.Rewrite(_exportOptionsEditor.RootPath.Value, oldDirectory, newDirectory, action);
        _preGenerationEditor.WorkingDirectory.Value = RelativePathRebaser.Rewrite(_preGenerationEditor.WorkingDirectory.Value, oldDirectory, newDirectory, action);
        _postGenerationEditor.WorkingDirectory.Value = RelativePathRebaser.Rewrite(_postGenerationEditor.WorkingDirectory.Value, oldDirectory, newDirectory, action);
    }

    /// <inheritdoc />
    public void Close()
    {
        _logger.LogInformation("Closing project: {FilePath}", DocumentFilePath);

        IsTransitioning = true;

        _document = null;
        HasDocument = false;
        DocumentFilePath = null;
        DocumentEpoch = 0;

        // Reset editors to empty defaults so IsDirty returns to false.
        _metadataEditor.SetOriginalValues(new DependencyProjectMetadata());
        _solutionOptionsEditor.SetOriginalValues(new GeneratorSolutionOptions());
        _exportOptionsEditor.SetOriginalValues(new GeneratorExportOptions());
        _diagramOptionsEditor.SetOriginalValues(new GeneratorDiagramOptions());
        _preGenerationEditor.SetOriginalValues(new PreGenerationConfig());
        _restoreSolutionEditor.SetOriginalValues(true);
        _postGenerationEditor.SetOriginalValues(new PostGenerationConfig());

        IsTransitioning = false;
    }

    /// <summary>Flushes all editor wrappers to the underlying document.</summary>
    private void FlushAllEditors()
    {
        _logger.LogDebug("Flushing editor values to the document");

        _metadataEditor.FlushTo(_document!.Metadata);
        _solutionOptionsEditor.FlushTo(_document!.DiagramGenerator.Solution);
        _exportOptionsEditor.FlushTo(_document!.DiagramGenerator.Export);
        _diagramOptionsEditor.FlushTo(_document!.DiagramGenerator.Diagram);
        _preGenerationEditor.FlushTo(_document!.PreGeneration);
        _document.RestoreSolution = _restoreSolutionEditor.RestoreSolution.Value;
        _postGenerationEditor.FlushTo(_document!.PostGeneration);
    }

    /// <summary>Marks all editor wrappers as clean after a successful save.</summary>
    private void MarkAllEditorsClean()
    {
        _logger.LogDebug("Marking all editors as clean");

        _metadataEditor.SetOriginalValues(_document!.Metadata);
        _solutionOptionsEditor.SetOriginalValues(_document!.DiagramGenerator.Solution);
        _exportOptionsEditor.SetOriginalValues(_document!.DiagramGenerator.Export);
        _diagramOptionsEditor.SetOriginalValues(_document!.DiagramGenerator.Diagram);
        _preGenerationEditor.SetOriginalValues(_document!.PreGeneration);
        _restoreSolutionEditor.SetOriginalValues(_document!.RestoreSolution);
        _postGenerationEditor.SetOriginalValues(_document!.PostGeneration);

        _logger.LogDebug("All editors are reset");
    }

    /// <inheritdoc />
    public DependencyGeneratorConfig BuildGeneratorConfig()
    {
        return BuildDocument().DiagramGenerator;
    }

    /// <inheritdoc />
    public DependencyProjectDocument BuildDocument()
    {
        Throw<InvalidOperationException>.WhenNull(_document, "No project is loaded");

        _logger.LogDebug("Building document from current editor state");

        FlushAllEditors();

        // Resolve relative paths against the document directory so downstream consumers
        // receive absolute paths regardless of the current working directory.
        if (DocumentFilePath is not null)
        {
            var docDir = Path.GetDirectoryName(DocumentFilePath)!;

            _document.DiagramGenerator.Solution.SolutionPath = PathUtils.ResolveAsAbsolutePath(_document.DiagramGenerator.Solution.SolutionPath, docDir);
            _document.DiagramGenerator.Export.RootPath = PathUtils.ResolveAsAbsolutePath(_document.DiagramGenerator.Export.RootPath, docDir);
        }

        return _document;
    }
}
