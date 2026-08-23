using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.ProcessExecution;

namespace SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;

/// <summary>Provides the ability to execute an optional post-generation command after diagram generation completes.</summary>
public interface IPostGenerationCommandRunner : IStudioScopedDependency
{
    /// <summary>
    /// Standard output lines from the running command, streamed as they arrive.
    /// Subscribe before calling <see cref="RunAsync"/> to receive real-time output.
    /// </summary>
    IObservable<string> StdOut { get; }

    /// <summary>
    /// Standard error lines from the running command, streamed as they arrive.
    /// Subscribe before calling <see cref="RunAsync"/> to receive real-time error output.
    /// </summary>
    IObservable<string> StdErr { get; }

    /// <summary>Executes the post-generation command specified in the provided configuration.</summary>
    /// <param name="config">The post-generation command configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="PostGenerationCommandResult"/> describing the outcome of the command execution.</returns>
    Task<PostGenerationCommandResult> RunAsync(PostGenerationConfig config, CancellationToken cancellationToken);
}
