using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Output;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs a dry-run analysis of the current project configuration.
/// Results are streamed as <see cref="OutputMessage"/> events via the returned observable.
/// </summary>
public interface IPreGenerationAnalysisService : IStudioSingletonDependency
{
    /// <summary>
    /// Runs the analysis and returns an observable that emits output messages
    /// as discovery progresses. The observable completes when analysis is finished.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An observable sequence of <see cref="OutputMessage"/> events.</returns>
    IObservable<OutputMessage> RunAsync(CancellationToken cancellationToken);
}
