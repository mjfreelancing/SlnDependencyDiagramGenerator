using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Shared.PreGeneration;

/// <summary>Provides the ability to execute an optional pre-generation command before diagram generation starts.</summary>
public interface IPreGenerationCommandRunner : IStudioScopedDependency
{
    /// <summary>Executes the pre-generation command specified in the provided configuration.</summary>
    /// <param name="config">The pre-generation command configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="PreGenerationCommandResult"/> describing the outcome of the command execution.</returns>
    Task<PreGenerationCommandResult> RunAsync(PreGenerationConfig config, CancellationToken cancellationToken);
}
