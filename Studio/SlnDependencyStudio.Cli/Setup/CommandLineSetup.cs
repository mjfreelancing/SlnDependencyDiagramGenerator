using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Cli.Handlers.Validate;
using System.CommandLine;

namespace SlnDependencyStudio.Cli.Setup;

/// <summary>Builds the System.CommandLine command tree by registering subcommands via a fluent builder pattern.
/// Each <c>AddXxx</c> method registers a subcommand and wires it to its handler.</summary>
internal sealed class CommandLineSetup
{
    private readonly List<Command> _commands = [];
    private readonly Option<string> _configFileOption = CreateConfigFileOption(false);

    // The required --configFile instance shared by both subcommands. Only one subcommand can match per
    // invocation, so a single instance can be registered on each of them (System.CommandLine allows one
    // option instance on multiple commands); GetConfigFileValue reads it to resolve the supplied value
    // regardless of which command matched, falling back to the root's non-required copy.
    private readonly Option<string> _requiredConfigFileOption = CreateConfigFileOption(true);

    /// <summary>Creates a new <c>--configFile</c> / <c>--cf</c> option instance.</summary>
    /// <param name="required">Whether the option is required on the command it is registered on.</param>
    private static Option<string> CreateConfigFileOption(bool required) =>
        new("--configFile", "--cf")
        {
            Description = "Path to the configuration JSON file",
            Required = required
        };

    /// <summary>Adds the <c>validate</c> subcommand wired to the provided handler.</summary>
    /// <param name="handler">The validate command handler.</param>
    /// <param name="setExitCode">A callback invoked with the exit code returned by the handler.</param>
    /// <returns>This instance, for chaining.</returns>
    public CommandLineSetup AddValidate(ICommandLineValidateHandler handler, Action<int> setExitCode)
    {
        // The subcommand requires --configFile (the shared _requiredConfigFileOption), while the root copy
        // (_configFileOption) is not required so a bare invocation - or --cf without a subcommand - parses
        // cleanly and reaches the friendly root fallback instead of a terse "option is required" error.
        var command = new Command("validate", "Validate a configuration file without running generation")
        {
            _requiredConfigFileOption
        };

        // Use the token-aware SetAction overload so the handler receives the invocation's cancellation token
        // - the token passed to InvokeAsync. This decouples the setup instance from the shutdown token.
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var configFilename = parseResult.GetValue(_requiredConfigFileOption)!;
            var exitCode = await handler.HandleAsync(configFilename, cancellationToken);
            setExitCode(exitCode);
        });

        _commands.Add(command);

        return this;
    }

    /// <summary>Adds the <c>run</c> subcommand wired to the provided handler.</summary>
    /// <param name="handler">The run command handler.</param>
    /// <param name="setExitCode">A callback invoked with the exit code returned by the handler.</param>
    /// <returns>This instance, for chaining.</returns>
    public CommandLineSetup AddRun(ICommandLineRunHandler handler, Action<int> setExitCode)
    {
        var command = new Command("run", "Generate dependency diagrams from a configuration file")
        {
            _requiredConfigFileOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var configFilename = parseResult.GetValue(_requiredConfigFileOption)!;
            var exitCode = await handler.HandleAsync(configFilename, cancellationToken);

            setExitCode(exitCode);
        });

        _commands.Add(command);

        return this;
    }

    /// <summary>Builds the root command with all registered subcommands and the shared config file option.</summary>
    /// <param name="logger">The logger used by the root fallback action when no subcommand is matched.</param>
    /// <param name="verboseOption">The verbose option registered on all subcommands.</param>
    /// <returns>The configured <see cref="RootCommand"/>.</returns>
    public RootCommand Build(ILogger logger, out Option<bool> verboseOption)
    {
        verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Enable verbose logging"
        };

        var root = new RootCommand("SlnDependencyStudio CLI — dependency diagram generation")
        {
            _configFileOption
        };

        foreach (var command in _commands)
        {
            command.Add(verboseOption);
            root.Add(command);
        }

        // Fallback: fires when no subcommand is matched (a bare invocation, or --cf without a subcommand).
        // The returned exit code is surfaced by App via InvokeAsync's return value when no handler set an
        // exit code.
        root.SetAction(parseResult =>
        {
            logger.LogError("A command must be specified. Use 'run' or 'validate'.");

            return (int)StudioCliExitCode.CommandLineParseFailed;
        });

        return root;
    }

    /// <summary>Resolves the <c>--configFile</c> value from a parse result, regardless of which command
    /// matched. The subcommands share a single required instance while the root owns a non-required copy, so
    /// the matched command's instance carries the supplied value. Resolving via the registered option
    /// instances (rather than by name or alias) means the lookup cannot drift from the registered options.</summary>
    /// <param name="parseResult">The parse result to read the option value from.</param>
    /// <returns>The config file path, or <c>null</c> if none was supplied.</returns>
    public string? GetConfigFileValue(ParseResult parseResult) =>
        parseResult.GetValue(_requiredConfigFileOption) ?? parseResult.GetValue(_configFileOption);
}
