using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs a dry-run analysis of the current project configuration.
/// </summary>
public interface IPreGenerationAnalysisService : IStudioSingletonDependency
{
    /// <summary>
    /// Runs the analysis and returns a task that completes when analysis is finished.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when analysis is finished.</returns>
    Task RunAsync(CancellationToken cancellationToken);
}
