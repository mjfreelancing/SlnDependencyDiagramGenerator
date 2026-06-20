using AllOverIt.Assertion;
using AllOverIt.GenericHost;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Shared.Serialization;
using System.CommandLine;

namespace SlnDependencyStudio.Cli;

/// <summary>CLI entry point that parses commands and delegates to services.</summary>
internal sealed class App : ConsoleAppBase
{
    private readonly DependencyGenerator _generator;
    private readonly IDependencyProjectSerializer _serializer;
    private readonly ILogger<App> _logger;

    /// <summary>Initializes a new instance of <see cref="App"/>.</summary>
    /// <param name="generator">The dependency diagram generator.</param>
    /// <param name="serializer">The dependency project document serializer.</param>
    /// <param name="logger">The logger instance.</param>
    public App(DependencyGenerator generator, IDependencyProjectSerializer serializer, ILogger<App> logger)
    {
        _generator = generator.WhenNotNull();
        _serializer = serializer.WhenNotNull();
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
            // shared configFileOption instance from the parse result.  Even
            // though the same Option object appears on multiple commands,
            // GetValue resolves it based on the context of the matched command.
            var configFile = parseResult.GetValue(configFileOption)!;
            await HandleRunAsync(configFile);
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
            var configFile = parseResult.GetValue(configFileOption)!;
            await HandleValidate(configFile);
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
        var parseResult = root.Parse(Environment.GetCommandLineArgs()[1..]);

        var exitCode = await parseResult.InvokeAsync(cancellationToken: cancellationToken);
        ExitCode = exitCode;
    }

    private async Task HandleRunAsync(string configFile)
    {
        try
        {
            var document = await _serializer.DeserializeAsync(configFile);

            ResolveRelativePaths(document.GeneratorConfig, configFile);

            LogResolvedGeneratorConfiguration(configFile, document.GeneratorConfig);

            _logger.LogInformation("Generating diagrams...");

            await _generator.CreateDiagramsAsync(document.GeneratorConfig, CancellationToken.None);

            _logger.LogInformation("Generation complete.");
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
        }
        catch (FileNotFoundException exception)
        {
            _logger.LogError("File not found: {Message}", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError("Failed to load dependency project file: {Message}", exception.Message);
        }
    }

    private async Task HandleValidate(string configFile)
    {
        try
        {
            var document = await _serializer.DeserializeAsync(configFile);

            ResolveRelativePaths(document.GeneratorConfig, configFile);

            LogResolvedGeneratorConfiguration(configFile, document.GeneratorConfig);

            _generator.ValidateConfiguration(document.GeneratorConfig);

            _logger.LogInformation("Configuration is valid.");
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
        }
        catch (FileNotFoundException exception)
        {
            _logger.LogError("File not found: {Message}", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError("Failed to load dependency project file: {Message}", exception.Message);
        }
    }

    private void LogResolvedGeneratorConfiguration(string configFile, DependencyGeneratorConfig config)
    {
        _logger.LogInformation("Configuration file: {ConfigFilePath}", Path.GetFullPath(configFile));
        _logger.LogInformation("Resolved paths and options:");
        _logger.LogInformation("  Solution path : {SolutionPath}", config.Projects.SolutionPath);
        _logger.LogInformation("  Export root   : {ExportRoot}", config.Export.RootPath);
        _logger.LogInformation("  Clear contents: {ClearContents}", config.Export.ClearContents);
        _logger.LogInformation("  Diagram formats : {Formats}", string.Join(", ", config.Diagram.Formats));
        _logger.LogInformation("  Diagram direction: {Direction}", config.Diagram.Direction);
        _logger.LogInformation("  Group name  : {GroupName}", config.Diagram.GroupName);
        _logger.LogInformation("  Group alias : {GroupAlias}", config.Diagram.GroupNameAlias);
        _logger.LogInformation("  Grouping enabled: {GroupingEnabled}", config.Diagram.Grouping.Enabled);
        _logger.LogInformation("  Image formats: {ImageFormats}", string.Join(", ", config.Export.ImageFormats));

        _logger.LogInformation("  Project scopes:");

        _logger.LogInformation("    Individual — Enabled: {IndividualEnabled}, IncludeDeps: {IndividualIncludeDeps}, TransitiveDepth: {IndividualTransitiveDepth}",
            config.Projects.Individual.Enabled,
            config.Projects.Individual.IncludeDependencies,
            config.Projects.Individual.TransitiveDepth);

        _logger.LogInformation("    All        — Enabled: {AllEnabled}, IncludeDeps: {AllIncludeDeps}, TransitiveDepth: {AllTransitiveDepth}",
            config.Projects.All.Enabled,
            config.Projects.All.IncludeDependencies,
            config.Projects.All.TransitiveDepth);

        _logger.LogInformation("  Regex include: {RegexInclude}", string.Join(", ", config.Projects.RegexToInclude));
        _logger.LogInformation("  Regex exclude: {RegexExclude}", string.Join(", ", config.Projects.RegexToExclude));
        _logger.LogInformation("  Packages to exclude: {PackagesExclude}", string.Join(", ", config.Projects.PackagesToExclude));
        _logger.LogInformation("  Frameworks to exclude: {FrameworksExclude}", string.Join(", ", config.Projects.FrameworksToExclude));
    }

    private static void ResolveRelativePaths(DependencyGeneratorConfig config, string configFilePath)
    {
        var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configFilePath))
            ?? throw new InvalidOperationException($"Cannot determine directory from path: {configFilePath}");

        if (!Path.IsPathRooted(config.Projects.SolutionPath))
        {
            config.Projects.SolutionPath = Path.GetFullPath(
                Path.Combine(configDirectory, config.Projects.SolutionPath));
        }

        if (!Path.IsPathRooted(config.Export.RootPath))
        {
            config.Export.RootPath = Path.GetFullPath(
                Path.Combine(configDirectory, config.Export.RootPath));
        }
    }

    private void WriteValidationErrors(ValidationException exception)
    {
        _logger.LogError("Configuration validation failed:");

        foreach (var error in exception.Errors)
        {
            _logger.LogError("  - {ErrorMessage}", error.ErrorMessage);
        }
    }
}