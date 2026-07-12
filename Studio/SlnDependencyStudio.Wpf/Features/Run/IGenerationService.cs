using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Output;

namespace SlnDependencyStudio.Wpf.Features.Run;

/// <summary>
/// Runs the full generation pipeline: pre-generation command (if enabled),
/// then <c>CreateDiagramsAsync</c>. Results are streamed as <see cref="OutputMessage"/>
/// events via the returned observable.
/// </summary>
public interface IGenerationService : IStudioSingletonDependency
{
    /// <summary>
    /// Runs generation and returns an observable that emits output messages
    /// as the pipeline progresses. The observable completes when generation is finished.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An observable sequence of <see cref="OutputMessage"/> events.</returns>
    IObservable<OutputMessage> RunAsync(CancellationToken cancellationToken);
}
