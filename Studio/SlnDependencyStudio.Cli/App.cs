using AllOverIt.GenericHost;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly CommandLineArguments _arguments;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<App> _logger;

    /// <summary>Initializes a new instance of <see cref="App"/>.</summary>
    /// <param name="arguments">The command-line arguments (excluding the executable name).</param>
    /// <param name="scopeFactory">The service scope factory, used to resolve the scoped command handlers for each run.</param>
    /// <param name="levelSwitch">The logging level switch (registered by <c>UseStudioSerilog</c>).</param>
    /// <param name="logger">The logger instance.</param>
    public App(CommandLineArguments arguments, IServiceScopeFactory scopeFactory, LoggingLevelSwitch levelSwitch, ILogger<App> logger)
    {
        _arguments = arguments;
        _scopeFactory = scopeFactory;
        _levelSwitch = levelSwitch;
        _logger = logger;
    }

    /// <inheritdoc />
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        return StartAsync(_arguments.Args, cancellationToken);
    }

    /// <summary>Runs the CLI with the given command-line arguments and shutdown token.</summary>
    /// <param name="args">The command-line arguments (excluding the executable name).</param>
    /// <param name="cancellationToken">A token that cancels when shutdown is requested (e.g. Ctrl+C/SIGTERM).</param>
    internal async Task StartAsync(string[] args, CancellationToken cancellationToken)
    {
        _logger.LogInformation("SlnDependencyStudio CLI started");

        // App is a singleton, but the command handlers (and their dependencies) are registered Scoped.
        // Resolve them from an explicit scope so the scoped graph is created and disposed per command -
        // a singleton resolving scoped services from the root would be a captive dependency.
        using var scope = _scopeFactory.CreateScope();

        var validateCommandHandler = scope.ServiceProvider.GetRequiredService<ICommandLineValidateHandler>();
        var runCommandHandler = scope.ServiceProvider.GetRequiredService<ICommandLineRunHandler>();

        // AllOverIt.GenericHost hands StartAsync a token linked against ApplicationStopping (held for the whole
        // command), so it cancels on Ctrl+C/SIGTERM. The token is passed to InvokeAsync below; System.CommandLine
        // forwards it to the command action via the token-aware SetAction overload (as a token linked to the one
        // given here), so the in-flight command and its subprocesses are cancelled on shutdown without
        // CommandLineSetup needing to hold the token itself.
        // The setup instance builds the command tree and owns the shared options, so it is kept
        // around to query parsed values (such as ConfigFileOption) once parsing has completed.
        var setup = new CommandLineSetup();

        var root = setup
            .AddValidate(validateCommandHandler, exitCode => ExitCode = exitCode)
            .AddRun(runCommandHandler, exitCode => ExitCode = exitCode)
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

                // System.CommandLine 2.0.8 defaults ProcessTerminationTimeout to 2 seconds, which arms a
                // ProcessTerminationHandler: on Ctrl+C it forces InvokeAsync to the signal's native exit code
                // (130 for SIGINT) if the in-flight command has not completed within that window - bypassing
                // the handler's own exit code. All handlers here are cancellation-aware via the shutdown
                // token threaded through CommandLineSetup above, so the override is disabled and cancellation
                // flows through the handler's OCE -> rethrow path to the catches below.
                //
                // The default exception handler must also be disabled: InvokeAsync otherwise catches any
                // exception thrown by the command action, prints it to stderr, and returns 1 - which would
                // swallow both the OCE (mapped to UserCancelled/OperationCancelled below) and genuine
                // failures (mapped to UnhandledCliFailure), collapsing every escaped exception to 1.
                var invocationConfiguration = new InvocationConfiguration
                {
                    ProcessTerminationTimeout = null,
                    EnableDefaultExceptionHandler = false
                };
                var actionExitCode = await parseResult.InvokeAsync(invocationConfiguration, cancellationToken: cancellationToken);

                // If no action ran (e.g. --help) and no handler set an exit code, default to success.
                // Only a null ExitCode is overwritten, so an exit code set by a handler is preserved.
                ExitCode ??= actionExitCode;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The shutdown token (linked against ApplicationStopping by AllOverIt.GenericHost) fired, so the
            // user pressed Ctrl+C/SIGTERM. This is distinct from a failure - the command was interrupted.
            _logger.LogWarning("Command cancelled by the user.");
            ExitCode = (int)StudioCliExitCode.UserCancelled;
        }
        catch (OperationCanceledException)
        {
            // The user's shutdown token did not fire, so an operation cancelled itself internally. Kept
            // distinct from a user-requested shutdown so scripts can tell the two apart.
            _logger.LogWarning("An operation was cancelled internally.");
            ExitCode = (int)StudioCliExitCode.OperationCancelled;
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
        // Fired by the host when Ctrl+C / SIGTERM triggers shutdown. The token handed to StartAsync
        // (linked against ApplicationStopping) is cancelled, so any in-flight command is cancelled.
        _logger.LogInformation("Shutdown requested - cancelling any in-flight command.");
    }
}
