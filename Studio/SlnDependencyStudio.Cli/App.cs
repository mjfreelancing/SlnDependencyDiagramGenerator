using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.GenericHost;
using AllOverIt.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Shared.Config.Extensions;
using SlnDependencyStudio.Shared.PreGeneration;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Shared.Validators.Contexts;
using System.CommandLine;

namespace SlnDependencyStudio.Cli;

/// <summary>CLI entry point that parses commands and delegates to services.</summary>
internal sealed class App : ConsoleAppBase
{
    private readonly DependencyGenerator _generator;
    private readonly IDependencyProjectSerializer _serializer;
    private readonly IPreGenerationCommandRunner _preGenerationCommandRunner;
    private readonly IValidationInvoker _validationInvoker;
    private readonly ILogger<App> _logger;

    /// <summary>Initializes a new instance of <see cref="App"/>.</summary>
    /// <param name="generator">The dependency diagram generator.</param>
    /// <param name="serializer">The dependency project document serializer.</param>
    /// <param name="preGenerationCommandRunner">The pre-generation command runner.</param>
    /// <param name="logger">The logger instance.</param>
    public App(DependencyGenerator generator, IDependencyProjectSerializer serializer, IPreGenerationCommandRunner preGenerationCommandRunner,
        IValidationInvoker validationInvoker, ILogger<App> logger)
    {
        _generator = generator.WhenNotNull();
        _serializer = serializer.WhenNotNull();
        _preGenerationCommandRunner = preGenerationCommandRunner.WhenNotNull();
        _validationInvoker = validationInvoker.WhenNotNull();
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

            // cancellationToken is captured from StartAsync's parameter and
            // propagated through the pre-generation command and generator call.
            await HandleRunAsync(configFile, cancellationToken);
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

    private async Task HandleRunAsync(string configFilename, CancellationToken cancellationToken)
    {
        try
        {
            // VALIDATION

            var configDirectory = GetConfigDirectory(configFilename);
            var document = await _serializer.DeserializeAsync(configFilename);

            ResolveRelativePaths(document.DiagramGenerator, configDirectory);

            document.LogConfiguration(configFilename, _logger);


            // Validate Pre-Generation Command settings.
            var preGenConfigContext = new PreGenerationConfigContext { ConfigDirectory = configDirectory };
            _validationInvoker.AssertValidation(document.PreGeneration, preGenConfigContext);

            // Validate the main diagram generator configuration.
            _generator.ValidateConfiguration(document.DiagramGenerator);




            // ── Pre-generation command ────────────────────────────────────
            // If the pre-generation command is enabled and non-empty, execute
            // it before running the generator.  On failure, the continue-on-
            // failure setting determines whether generation still proceeds.
            // The working directory is resolved to an absolute path before
            // being passed to the runner.
            var preGenConfig = document.PreGeneration;

            if (preGenConfig.Enabled && preGenConfig.Command.IsNotNullOrEmpty())
            {
                var resolvedWorkingDir = preGenConfig.WorkingDirectory.IsNotNullOrEmpty()
                    ? PathUtils.ResolveAsAbsolutePath(preGenConfig.WorkingDirectory, configDirectory)
                    : null;

                // Assign the resolved path so the runner uses the absolute form
                preGenConfig.WorkingDirectory = resolvedWorkingDir ?? string.Empty;

                _logger.LogInformation(
                    "Running pre-generation command: {Command} {Arguments} (WorkingDirectory: {WorkingDirectory}, ContinueOnFailure: {ContinueOnFailure})",
                    preGenConfig.Command,
                    preGenConfig.Arguments,
                    resolvedWorkingDir ?? "<default>",
                    preGenConfig.ContinueOnFailure);

                var preGenResult = await _preGenerationCommandRunner.RunAsync(preGenConfig, cancellationToken);

                if (!preGenResult.Succeeded)
                {
                    if (!preGenConfig.ContinueOnFailure)
                    {
                        _logger.LogError(
                            "Pre-generation command failed and continue-on-failure is disabled. Aborting.\n  {ErrorMessage}",
                            preGenResult.ErrorMessage);

                        ExitCode = StudioCliExitCode.PreGenerationCommandFailed.Value;
                        return;
                    }

                    _logger.LogWarning(
                        "Pre-generation command failed but continue-on-failure is enabled. Proceeding with generation.\n  {ErrorMessage}",
                        preGenResult.ErrorMessage);
                }
            }

            // ── Diagram generation ────────────────────────────────────────
            _logger.LogInformation("Generating diagrams...");

            await _generator.CreateDiagramsAsync(document.DiagramGenerator, cancellationToken);

            _logger.LogInformation("Generation complete.");

            ExitCode = 0;
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
            ExitCode = StudioCliExitCode.ValidateCommandFailed.Value;
        }
        catch (FileNotFoundException exception)
        {
            _logger.LogError("File not found: {Message}", exception.Message);
            ExitCode = StudioCliExitCode.ConfigFileNotFound.Value;
        }
        catch (DependencyGeneratorException exception)
        {
            _logger.LogError("Diagram generator failed: {Message}", exception.Message);
            ExitCode = StudioCliExitCode.DiagramGeneratorFailed.Value;
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError("Failed to load dependency project file: {Message}", exception.Message);
            ExitCode = StudioCliExitCode.RunCommandFailed.Value;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was cancelled.");
            ExitCode = StudioCliExitCode.RunCommandFailed.Value;
        }
    }

    private async Task HandleValidate(string configFilename)
    {
        try
        {
            var configDirectory = GetConfigDirectory(configFilename);
            var document = await _serializer.DeserializeAsync(configFilename);

            ResolveRelativePaths(document.DiagramGenerator, configDirectory);

            document.LogConfiguration(configFilename, _logger);


            // Validate Pre-Generation Command settings.
            var preGenConfigContext = new PreGenerationConfigContext { ConfigDirectory = configDirectory };
            _validationInvoker.AssertValidation(document.PreGeneration, preGenConfigContext);

            // Validate the main diagram generator configuration.
            _generator.ValidateConfiguration(document.DiagramGenerator);



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

    private static string GetConfigDirectory(string configFilePath)
    {
        return Path.GetDirectoryName(Path.GetFullPath(configFilePath))
            ?? throw new InvalidOperationException($"Cannot determine directory from path: {configFilePath}");
    }

    private static void ResolveRelativePaths(DependencyGeneratorConfig config, string configDirectory)
    {
        config.Projects.SolutionPath = PathUtils.ResolveAsAbsolutePath(config.Projects.SolutionPath, configDirectory);
        config.Export.RootPath = PathUtils.ResolveAsAbsolutePath(config.Export.RootPath, configDirectory);
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
