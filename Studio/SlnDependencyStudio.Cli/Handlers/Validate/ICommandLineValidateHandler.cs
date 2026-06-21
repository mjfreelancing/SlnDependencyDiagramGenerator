using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Cli.Handlers.Validate;

public interface ICommandLineValidateHandler : IStudioScopedDependency
{
    Task<int> HandleAsync(string configFilename, CancellationToken cancellationToken);
}
