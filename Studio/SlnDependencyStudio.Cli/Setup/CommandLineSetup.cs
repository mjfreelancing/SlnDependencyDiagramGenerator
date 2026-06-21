using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Cli.Handlers.Validate;
using System.CommandLine;

namespace SlnDependencyStudio.Cli.Setup;

/// <summary>Builds the System.CommandLine command tree by registering subcommands via a fluent builder pattern.
/// Each <c>AddXxx</c> method registers a subcommand and wires it to its handler.</summary>
internal sealed class CommandLineSetup
{
    private readonly CancellationToken _cancellationToken;
    private readonly List<Command> _commands = [];

    /// <summary>Initializes a new instance of <see cref="CommandLineSetup"/>.</summary>
    /// <param name="cancellationToken">The cancellation token passed to all handlers.</param>
    public CommandLineSetup(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    /// <summary>Creates a new, shared, <c>--configFile</c> / <c>--cf</c> option instance.</summary>
    private static Option<string> CreateConfigFileOption() =>
        new("--configFile", "--cf")
        {
            Description = "Path to the configuration JSON file",
            Required = true
        };

    /// <summary>Adds the <c>validate</c> subcommand wired to the provided handler.</summary>
    /// <param name="handler">The validate command handler.</param>
    /// <param name="setExitCode">A callback invoked with the exit code returned by the handler.</param>
    /// <returns>This instance, for chaining.</returns>
    public CommandLineSetup AddValidate(ICommandLineValidateHandler handler, Action<int> setExitCode)
    {
        var configFileOption = CreateConfigFileOption();

        var command = new Command("validate", "Validate a configuration file without running generation")
        {
            configFileOption
        };

        command.SetAction(async parseResult =>
        {
            var configFilename = parseResult.GetValue(configFileOption)!;
            setExitCode(await handler.HandleAsync(configFilename, _cancellationToken));
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
        var configFileOption = CreateConfigFileOption();

        var command = new Command("run", "Generate dependency diagrams from a configuration file")
        {
            configFileOption
        };

        command.SetAction(async parseResult =>
        {
            var configFilename = parseResult.GetValue(configFileOption)!;
            setExitCode(await handler.HandleAsync(configFilename, _cancellationToken));
        });

        _commands.Add(command);

        return this;
    }

    /// <summary>Builds the root command with all registered subcommands and the shared config file option.</summary>
    /// <param name="logger">The logger used by the root fallback action when no subcommand is matched.</param>
    /// <returns>The configured <see cref="RootCommand"/>.</returns>
    public RootCommand Build(ILogger logger)
    {
        var root = new RootCommand("SlnDependencyStudio CLI — dependency diagram generation")
        {
            CreateConfigFileOption()
        };

        foreach (var command in _commands)
        {
            root.Add(command);
        }

        // Fallback: fires when --cf is provided without a subcommand
        root.SetAction(parseResult =>
        {
            logger.LogError("A command must be specified. Use 'run' or 'validate'.");
        });

        return root;
    }
}
