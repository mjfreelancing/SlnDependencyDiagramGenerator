using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Serialization;

namespace SlnDependencyStudio.Cli.Handlers.Run;

internal sealed class CommandLineRunHandler : CommandLineHandlerBase, ICommandLineRunHandler
{
    public CommandLineRunHandler(IDependencyProjectSerializer serializer, ILogger<CommandLineRunHandler> logger)
        : base(serializer, logger)
    {
    }

    public override Task<int> HandleAsync(string configFilename, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
