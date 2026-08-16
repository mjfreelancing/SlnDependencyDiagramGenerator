using AllOverIt.GenericHost;
using Microsoft.Extensions.Hosting;
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
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<App> _logger;

    /// <summary>Initializes a new instance of <see cref="App"/>.</summary>
    /// <param name="validateCommandHandler">The Validate command handler.</param>
    /// <param name="runCommandHandler">The Run command handler.</param>
    /// <param name="applicationLifetime">The host application lifetime, used to observe shutdown requests (e.g. Ctrl+C).</param>
    /// <param name="levelSwitch">The logging level switch (registered by <c>UseStudioSerilog</c>).</param>
    /// <param name="logger">The logger instance.</param>
    public App(ICommandLineValidateHandler validateCommandHandler, ICommandLineRunHandler runCommandHandler,
        IHostApplicationLifetime applicationLifetime, LoggingLevelSwitch levelSwitch, ILogger<App> logger)
    {
        _validateCommandHandler = validateCommandHandler;
        _runCommandHandler = runCommandHandler;
        _applicationLifetime = applicationLifetime;
        _levelSwitch = levelSwitch;
        _logger = logger;
    }

    /// <inheritdoc />
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        // The public override reads the process command line; the args are threaded through the
        // internal overload (below) so tests can inject their own argv instead of the runner's.
        return StartAsync(Environment.GetCommandLineArgs()[1..], cancellationToken);
    }

    /// <summary>Runs the CLI with the given command-line arguments and shutdown token.</summary>
    /// <param name="args">The command-line arguments (excluding the executable name).</param>
    /// <param name="cancellationToken">The host startup token (see the note below for why it is not used directly).</param>
    internal async Task StartAsync(string[] args, CancellationToken cancellationToken)
    {
        _logger.LogInformation("SlnDependencyStudio CLI started");

        // The token handed to StartAsync by AllOverIt.GenericHost is a linked token (startup + ApplicationStopping)
        // created inside Host.StartAsync, and its CancellationTokenSource is disposed as soon as ApplicationStarted
        // fires - which severs the ApplicationStopping registration. By the time the command actually runs the token
        // is frozen and never cancels on Ctrl+C. We therefore create our own linked token against ApplicationStopping,
        // held for the whole command, so Ctrl+C/SIGTERM cancels the in-flight command and its subprocesses.
        using var shutdownTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _applicationLifetime.ApplicationStopping);

        var cancellation = shutdownTokenSource.Token;

        // The setup instance builds the command tree and owns the shared options, so it is kept
        // around to query parsed values (such as ConfigFileOption) once parsing has completed.
        var setup = new CommandLineSetup(cancellation);

        var root = setup
            .AddValidate(_validateCommandHandler, exitCode => ExitCode = exitCode)
            .AddRun(_runCommandHandler, exitCode => ExitCode = exitCode)
            .Build(_logger, out var verboseOption);

        var parseResult = root.Parse(args);

        // Set the logging level as early as possible, before any handler runs.
        var isVerbose = parseResult.GetValue(verboseOption);

        if (isVerbose)
        {
            _levelSwitch.MinimumLevel = LogEventLevel.Debug;
        }

        _logger.LogDebug("Verbose logging enabled: {Verbose}", isVerbose);

        _logger.LogDebug("Command line arguments: {Arguments}", string.Join(' ', args));

        // Only read parsed option values once parsing has succeeded. A required-but-missing option
        // (e.g. a bare `studio` invocation that omits --configFile) makes GetValue throw
        // InvalidOperationException. That must surface as a parse error (exit 1001) in the try block
        // below, not escape StartAsync and be reported as an unhandled stack trace by the host.
        if (parseResult.Errors.Count == 0)
        {
            // Log the selected command and the config file path supplied on the command line (if any).
            _logger.LogDebug("Selected command: {Command}, config file: {ConfigFile}",
                parseResult.CommandResult.Command.Name, parseResult.GetValue(setup.ConfigFileOption) ?? "<none>");
        }

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
                var actionExitCode = await parseResult.InvokeAsync(cancellationToken: cancellation);

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

    /// <inheritdoc />
    public override void OnStopping()
    {
        // Fired by the host when Ctrl+C / SIGTERM triggers shutdown. The linked token created in
        // StartAsync is cancelled via ApplicationStopping, so any in-flight command is cancelled.
        _logger.LogInformation("Shutdown requested - cancelling any in-flight command.");
    }
}
