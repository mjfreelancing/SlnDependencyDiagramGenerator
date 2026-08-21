using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Cli.Handlers.Run;

/// <summary>Handles the <c>run</c> command which executes pre-generation commands and generates dependency diagrams.</summary>
internal interface ICommandLineRunHandler : IStudioScopedDependency
{
    /// <summary>Runs the pre-generation command (if configured) and then generates dependency diagrams.</summary>
    /// <param name="projectFilename">The path to the project file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that resolves to the exit code: 0 on success, or a <see cref="StudioCliExitCode"/> value on failure.</returns>
    Task<int> HandleAsync(string projectFilename, CancellationToken cancellationToken);
}
