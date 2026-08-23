using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs the full generation pipeline: restores the solution (if enabled), runs the pre-generation
/// command (if enabled), generates the diagrams, then runs the post-generation command (if enabled).
/// </summary>
public interface IGenerationService : IStudioSingletonDependency
{
    /// <summary>
    /// Runs generation and returns a task that completes when generation is finished.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when generation is finished.</returns>
    Task RunAsync(CancellationToken cancellationToken);
}
