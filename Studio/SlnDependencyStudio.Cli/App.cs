using AllOverIt.Assertion;
using AllOverIt.GenericHost;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Cli.Handlers.Validate;
using System.CommandLine;

namespace SlnDependencyStudio.Cli;

/// <summary>CLI entry point that parses commands and delegates to services.</summary>
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
        // ── Option definition ──────────────────────────────────────────────
        // Defines a CLI option that accepts both a long form (--configFile)
        // and a short alias (--cf). Required = true means the parser will
        // report an error if it is missing when the owning command is invoked.
        // This single Option<string> instance is shared (re-used) across
        // multiple commands below. System.CommandLine resolves the option's
        // value from whichever command context the parser ends up matching.
        var configFileOption = new Option<string>("--configFile", "--cf")
        {
            Description = "Path to the configuration JSON file",
            Required = true
        };

        // ── "run" subcommand ──────────────────────────────────────────────
        // A named subcommand.  When the user types `run --cf file.sds` the
        // parser matches this command and invokes its SetAction handler.
        // The configFileOption is added to this command's option list so that
        // --cf / --configFile is recognised when used under `run`.
        var runCommand = new Command("run", "Generate dependency diagrams from a configuration file")
        {
            configFileOption
        };

        runCommand.SetAction(async parseResult =>
        {
            // parseResult.GetValue(configFileOption) reads the value of the
            // shared configFileOption instance from the parse result. Even
            // though the same Option object appears on multiple commands,
            // GetValue resolves it based on the context of the matched command.
            var configFilename = parseResult.GetValue(configFileOption)!;

            ExitCode = await _runCommandHandler.HandleAsync(configFilename, cancellationToken);
        });

        // ── "validate" subcommand ─────────────────────────────────────────
        // Same pattern as `run`. The configFileOption is added to this
        // command as well so it is recognised under `validate`.
        var validateCommand = new Command("validate", "Validate a configuration file without running generation")
        {
            configFileOption
        };

        validateCommand.SetAction(async parseResult =>
        {
            var configFilename = parseResult.GetValue(configFileOption)!;

            ExitCode = await _validateCommandHandler.HandleAsync(configFilename, cancellationToken);
        });

        // ── Root command ──────────────────────────────────────────────────
        // The root command contains the subcommands plus the shared option.
        // configFileOption is added to the root only so the root fallback
        // action (below) can run. Without it, the parser rejects `--cf` as
        // unrecognised before any action executes, so we could never show
        // the helpful "use 'run' or 'validate'" message. The fallback
        // serves no purpose other than providing that clearer error.
        var root = new RootCommand("SlnDependencyStudio CLI — dependency diagram generation")
        {
            runCommand,
            validateCommand,
            configFileOption
        };

        // ── Fallback root action ──────────────────────────────────────────
        // SetAction on the root command acts as a fallback: it is invoked
        // ONLY when NO subcommand matched (e.g. `--cf file.sds` with neither
        // `run` nor `validate`). When a subcommand IS matched, that subcommand's
        // SetAction runs instead and the root action is skipped.
        // See https://learn.microsoft.com/en-us/dotnet/standard/commandline/
        root.SetAction(parseResult =>
        {
            _logger.LogError("A command must be specified. Use 'run' or 'validate'.");
        });

        // ── Parse and invoke ──────────────────────────────────────────────
        // root.Parse() tokenises the raw CLI args and matches them against the
        // command tree (root → subcommands). The result captures which command (if any) was
        // matched, which options were provided, and any errors. InvokeAsync() then executes
        // the matched command's SetAction handler (or the root fallback if nothing matched).
        //
        // ExitCode is set explicitly by each handler on failure; on success it stays 0.
        // InvokeAsync's return value is NOT used because it would overwrite the specific
        // exit codes our handlers already assigned.
        try
        {
            var parseResult = root.Parse(Environment.GetCommandLineArgs()[1..]);

            if (parseResult.Errors.Count > 0)
            {
                ExitCode = StudioCliExitCode.CommandLineParseFailed.Value;
            }
            else
            {
                await parseResult.InvokeAsync(cancellationToken: cancellationToken);
                ExitCode = 0;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unexpected CLI failure occurred.");
            ExitCode = StudioCliExitCode.UnhandledCliFailure.Value;
        }
    }
}
