using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Renderers;

/// <summary>
/// Takes a resolved <see cref="DependencyGraphModel"/> and emits renderer-specific diagram artifacts.
/// </summary>
internal interface IDiagramRenderer
{
    /// <summary>
    /// The file extension for this renderer's output (without the leading dot).
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Validates that any required external tools are available.
    /// </summary>
    /// <param name="imageExportEnabled">
    /// <see langword="true"/> when image export is requested; otherwise <see langword="false"/>.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ValidateRequiredToolsAsync(bool imageExportEnabled, CancellationToken cancellationToken);

    /// <summary>
    /// Renders and writes the diagram file, and optionally exports images.
    /// </summary>
    /// <param name="targetFramework">The target framework being processed.</param>
    /// <param name="exportPath">The framework-specific export root path.</param>
    /// <param name="projectScope">The current project scope name (individual project or grouped-all scope).</param>
    /// <param name="model">The resolved dependency graph model.</param>
    /// <param name="imageFormats">The optional image formats to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task CreateDiagramArtifactsAsync(string targetFramework, string exportPath, string projectScope,
        DependencyGraphModel model, DiagramImageFormat[] imageFormats, CancellationToken cancellationToken);

    /// <summary>
    /// Renders the dependency graph and returns the diagram file content.
    /// </summary>
    /// <param name="model">The resolved dependency graph model.</param>
    string Render(DependencyGraphModel model);
}