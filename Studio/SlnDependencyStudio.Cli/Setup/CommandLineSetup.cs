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
    private readonly Option<string> _projectFileOption = CreateProjectFileOption(false);

    // The required --projectFile instance shared by both subcommands. Only one subcommand can match per
    // invocation, so a single instance can be registered on each of them (System.CommandLine allows one
    // option instance on multiple commands); GetProjectFileValue reads it to resolve the supplied value
    // regardless of which command matched, falling back to the root's non-required copy.
    private readonly Option<string> _requiredProjectFileOption = CreateProjectFileOption(true);

    /// <summary>Adds the <c>validate</c> subcommand wired to the provided handler.</summary>
    /// <param name="handler">The validate command handler.</param>
    /// <param name="setExitCode">A callback invoked with the exit code returned by the handler.</param>
    /// <returns>This instance, for chaining.</returns>
    public CommandLineSetup AddValidate(ICommandLineValidateHandler handler, Action<int> setExitCode)
    {
        // The subcommand requires --projectFile (the shared _requiredProjectFileOption), while the root copy
        // (_projectFileOption) is not required so a bare invocation - or --pf without a subcommand - parses
        // cleanly and reaches the friendly root fallback instead of a terse "option is required" error.
        var command = new Command("validate", "Validate a dependency project file without running generation")
        {
            _requiredProjectFileOption
        };

        // Use the token-aware SetAction overload so the handler receives the invocation's cancellation token
        // - the token passed to InvokeAsync. This decouples the setup instance from the shutdown token.
        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var projectFilename = parseResult.GetValue(_requiredProjectFileOption)!;
            var exitCode = await handler.HandleAsync(projectFilename, cancellationToken);
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
        var command = new Command("run", "Generate dependency diagrams from a dependency project file")
        {
            _requiredProjectFileOption
        };

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var projectFilename = parseResult.GetValue(_requiredProjectFileOption)!;
            var exitCode = await handler.HandleAsync(projectFilename, cancellationToken);

            setExitCode(exitCode);
        });

        _commands.Add(command);

        return this;
    }

    /// <summary>Builds the root command with all registered subcommands and the shared project file option.</summary>
    /// <param name="logger">The logger used by the root fallback action when no subcommand is matched.</param>
    /// <param name="verboseOption">The recursive verbose option registered on the root.</param>
    /// <returns>The configured <see cref="RootCommand"/>.</returns>
    public RootCommand Build(ILogger logger, out Option<bool> verboseOption)
    {
        // --verbose is recursive and registered on the root only, so it is valid both before and after
        // the subcommand (e.g. `studio --verbose run` and `studio run --verbose`) - the same way
        // System.CommandLine treats --help/--version as global options.
        verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Enable verbose logging",
            Recursive = true
        };

        var root = new RootCommand("SlnDependencyStudio CLI — dependency diagram generation")
        {
            _projectFileOption,
            verboseOption
        };

        foreach (var command in _commands)
        {
            root.Add(command);
        }

        // Fallback: fires when no subcommand is matched (a bare invocation, or --pf without a subcommand).
        // The returned exit code is surfaced by App via InvokeAsync's return value when no handler set an
        // exit code.
        root.SetAction(parseResult =>
        {
            logger.LogError("A command must be specified. Use 'run' or 'validate'.");

            return (int)StudioCliExitCode.CommandLineParseFailed;
        });

        return root;
    }

    /// <summary>Resolves the <c>--projectFile</c> value from a parse result, regardless of which command
    /// matched. The subcommands share a single required instance while the root owns a non-required copy, so
    /// the matched command's instance carries the supplied value. Resolving via the registered option
    /// instances (rather than by name or alias) means the lookup cannot drift from the registered options.</summary>
    /// <param name="parseResult">The parse result to read the option value from.</param>
    /// <returns>The project file path, or <c>null</c> if none was supplied.</returns>
    public string? GetProjectFileValue(ParseResult parseResult) =>
        parseResult.GetValue(_requiredProjectFileOption) ?? parseResult.GetValue(_projectFileOption);

    /// <summary>Creates a new <c>--projectFile</c> / <c>--pf</c> option instance.</summary>
    /// <param name="required">Whether the option is required on the command it is registered on.</param>
    private static Option<string> CreateProjectFileOption(bool required)
    {
        return new("--projectFile", "--pf")
        {
            Description = "Path to the dependency project (.sds) file",
            Required = required
        };
    }
}
