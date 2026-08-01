using AllOverIt.Assertion;
using AllOverIt.GenericHost;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Cli.Handlers.Validate;
using SlnDependencyStudio.Cli.Setup;
using System.CommandLine;

namespace SlnDependencyStudio.Cli;

/// <summary>CLI entry point that parses commands and delegates to registered handlers.</summary>
internal sealed class App : ConsoleAppBase
{
    private readonly ICommandLineValidateHandler _validateCommandHandler;
    private readonly ICommandLineRunHandler _runCommandHandler;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<App> _logger;

    /// <summary>Initializes a new instance of <see cref="App"/>.</summary>
    /// <param name="validateCommandHandler">The Validate command handler.</param>
    /// <param name="runCommandHandler">The Run command handler.</param>
    /// <param name="levelSwitch">The logging level switch (registered by <c>UseStudioSerilog</c>).</param>
    /// <param name="logger">The logger instance.</param>
    public App(ICommandLineValidateHandler validateCommandHandler, ICommandLineRunHandler runCommandHandler,
        LoggingLevelSwitch levelSwitch, ILogger<App> logger)
    {
        _validateCommandHandler = validateCommandHandler.WhenNotNull();
        _runCommandHandler = runCommandHandler.WhenNotNull();
        _levelSwitch = levelSwitch.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SlnDependencyStudio CLI started");

        var root = new CommandLineSetup(cancellationToken)
            .AddValidate(_validateCommandHandler, exitCode => ExitCode = exitCode)
            .AddRun(_runCommandHandler, exitCode => ExitCode = exitCode)
            .Build(_logger, out var verboseOption);

        var args = Environment.GetCommandLineArgs()[1..];
        var parseResult = root.Parse(args);

        // Set the logging level as early as possible, before any handler runs.
        var isVerbose = parseResult.GetValue(verboseOption);

        if (isVerbose)
        {
            _levelSwitch.MinimumLevel = LogEventLevel.Debug;
        }

        _logger.LogDebug("Verbose logging enabled: {Verbose}", isVerbose);
        _logger.LogDebug("Command line arguments: {Arguments}", string.Join(' ', args));

        try
        {
            if (parseResult.Errors.Count > 0)
            {
                ExitCode = (int)StudioCliExitCode.CommandLineParseFailed;
            }
            else
            {
                await parseResult.InvokeAsync(cancellationToken: cancellationToken);

                // If no action ran (e.g. --help) and no handler set an exit code, default to success.
                // This keeps ExitCode as null when a handler genuinely forgets to set it, so we can detect that bug.
                ExitCode ??= 0;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unexpected CLI failure occurred.");
            ExitCode = (int)StudioCliExitCode.UnhandledCliFailure;
        }

        _logger.LogInformation("SlnDependencyStudio CLI completed with exit code {ExitCode}.", ExitCode);
    }
}
