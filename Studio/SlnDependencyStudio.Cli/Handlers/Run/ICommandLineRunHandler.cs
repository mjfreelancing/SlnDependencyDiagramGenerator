using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Cli.Handlers.Run;

public interface ICommandLineRunHandler : IStudioScopedDependency
{
    Task<int> HandleAsync(string configFilename, CancellationToken cancellationToken);
}
