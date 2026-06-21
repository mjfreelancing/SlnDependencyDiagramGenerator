using AllOverIt.Assertion;
using AllOverIt.GenericHost;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Cli.Handlers.Validate;
using SlnDependencyStudio.Cli.Setup;

namespace SlnDependencyStudio.Cli;

/// <summary>CLI entry point that parses commands and delegates to registered handlers.</summary>
internal sealed class App : ConsoleAppBase
{
    private readonly ICommandLineValidateHandler _validateCommandHandler;
    private readonly ICommandLineRunHandler _runCommandHandler;
    private readonly ILogger<App> _logger;

    /// <summary>Initializes a new instance of <see cref="App"/>.</summary>
    /// <param name="validateCommandHandler">The Validate command handler.</param>
    /// <param name="runCommandHandler">The Run command handler.</param>
    /// <param name="logger">The logger instance.</param>
    public App(ICommandLineValidateHandler validateCommandHandler, ICommandLineRunHandler runCommandHandler, ILogger<App> logger)
    {
        _validateCommandHandler = validateCommandHandler.WhenNotNull();
        _runCommandHandler = runCommandHandler;
        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var root = new CommandLineSetup(cancellationToken)
            .AddValidate(_validateCommandHandler, exitCode => ExitCode = exitCode)
            .AddRun(_runCommandHandler, exitCode => ExitCode = exitCode)
            .Build(_logger);

        var parseResult = root.Parse(Environment.GetCommandLineArgs()[1..]);

        try
        {
            if (parseResult.Errors.Count > 0)
            {
                ExitCode = StudioCliExitCode.CommandLineParseFailed.Value;
            }
            else
            {
                await parseResult.InvokeAsync(cancellationToken: cancellationToken);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unexpected CLI failure occurred.");
            ExitCode = StudioCliExitCode.UnhandledCliFailure.Value;
        }
    }
}
