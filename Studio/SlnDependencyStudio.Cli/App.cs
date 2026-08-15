using AllOverIt.GenericHost;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
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
        _validateCommandHandler = validateCommandHandler;
        _runCommandHandler = runCommandHandler;
        _levelSwitch = levelSwitch;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SlnDependencyStudio CLI started");

        // The setup instance builds the command tree and owns the shared options, so it is kept
        // around to query parsed values (such as ConfigFileOption) once parsing has completed.
        var setup = new CommandLineSetup(cancellationToken);

        var root = setup
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

        // Log the selected command and the config file path supplied on the command line (if any).
        _logger.LogDebug("Selected command: {Command}, config file: {ConfigFile}",
            parseResult.CommandResult.Command.Name, parseResult.GetValue(setup.ConfigFileOption) ?? "<none>");

        _logger.LogDebug("Command line arguments: {Arguments}", string.Join(' ', args));

        try
        {
            if (parseResult.Errors.Count > 0)
            {
                foreach (var error in parseResult.Errors)
                {
                    _logger.LogError("Command line error: {Message}", error.Message);
                }

                ExitCode = (int)StudioCliExitCode.CommandLineParseFailed;
            }
            else
            {
                // InvokeAsync returns the command action's exit code (or 0). Handlers set ExitCode via the
                // setExitCode callback; the action's return value is used only when no exit code was set
                // (e.g. the root fallback action when no subcommand is specified).
                var actionExitCode = await parseResult.InvokeAsync(cancellationToken: cancellationToken);

                // If no action ran (e.g. --help) and no handler set an exit code, default to success.
                // Only a null ExitCode is overwritten, so an exit code set by a handler is preserved.
                ExitCode ??= actionExitCode;
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
