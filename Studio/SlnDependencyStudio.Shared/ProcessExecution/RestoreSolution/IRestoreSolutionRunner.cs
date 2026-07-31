using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.ProcessExecution;

namespace SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;

/// <summary>Provides the ability to restore a Visual Studio solution (via <c>dotnet restore</c>)
/// so that project assets are available before diagram generation starts.</summary>
public interface IRestoreSolutionRunner : IStudioScopedDependency
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

    /// <summary>Restores the specified solution using <c>dotnet restore</c>.</summary>
    /// <param name="solutionPath">The fully-qualified path to the solution file to restore.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="RestoreSolutionResult"/> describing the outcome of the restore.</returns>
    Task<RestoreSolutionResult> RunAsync(string solutionPath, CancellationToken cancellationToken);
}
