using SlnDependencyDiagramGenerator.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator;

public interface IDependencyGenerator
{
    /// <summary>
    /// Streams progress messages during diagram generation (project discovery,
    /// framework processing, diagram creation). Callers subscribe to receive progress
    /// without requiring verbose logging.
    /// </summary>
    IObservable<string> OnProgress { get; }

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
