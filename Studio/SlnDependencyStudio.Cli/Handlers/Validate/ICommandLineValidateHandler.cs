using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Cli.Handlers.Validate;

/// <summary>Handles the <c>validate</c> command which validates a dependency project file without running generation.</summary>
internal interface ICommandLineValidateHandler : IStudioScopedDependency
{
    /// <summary>Validates the configuration in the specified file.</summary>
    /// <param name="projectFilename">The path to the project file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that resolves to the exit code: 0 on success, or a <see cref="StudioCliExitCode"/> value on failure.</returns>
    Task<int> HandleAsync(string projectFilename, CancellationToken cancellationToken);
}
