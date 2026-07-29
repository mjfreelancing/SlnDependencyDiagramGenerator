using SlnDependencyDiagramGenerator.Config;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Generates dependency diagrams from solution and project metadata, producing output
/// in supported diagram formats (such as Mermaid and D2) along with optional images and dependency
/// summaries.</summary>
public interface IDependencyGenerator
{
    /// <summary>Validates a <see cref="DependencyGeneratorConfig"/> and throws <see cref="FluentValidation.ValidationException"/>
    /// if any rules are violated.</summary>
    /// <param name="configuration">The configuration to validate.</param>
    void ValidateConfiguration(DependencyGeneratorConfig configuration);

    /// <summary>Generates dependency summaries, diagram files, and optional images for each discovered target framework.</summary>
    /// <param name="configuration">The dependency generator configuration options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> that completes when the diagram generation has completed.</returns>
    Task CreateDiagramsAsync(DependencyGeneratorConfig configuration, CancellationToken cancellationToken);
}
