using AllOverIt.Assertion;
using AllOverIt.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Config.Extensions;
using SlnDependencyStudio.Shared.PreGeneration;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Validators.Contexts;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SlnDependencyStudio.Cli.Handlers.Run;

/// <inheritdoc cref="ICommandLineRunHandler"/>
internal sealed class CommandLineRunHandler : CommandLineHandlerBase, ICommandLineRunHandler
{
    private readonly IDependencyGenerator _generator;
    private readonly IPreGenerationCommandRunner _preGenerationCommandRunner;
    private readonly IValidationInvoker _validationInvoker;
    private readonly ILogger<CommandLineRunHandler> _logger;

    /// <summary>Initializes a new instance of <see cref="CommandLineRunHandler"/>.</summary>
    /// <param name="serializer">The dependency project document serializer.</param>
    /// <param name="generator">The dependency diagram generator.</param>
    /// <param name="preGenerationCommandRunner">The pre-generation command runner.</param>
    /// <param name="validationInvoker">The validation invoker for model validation.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandLineRunHandler(IDependencyProjectSerializer serializer, IDependencyGenerator generator,
        IPreGenerationCommandRunner preGenerationCommandRunner, IValidationInvoker validationInvoker,
        ILogger<CommandLineRunHandler> logger)
        : base(serializer, logger)
    {
        _generator = generator.WhenNotNull();
        _preGenerationCommandRunner = preGenerationCommandRunner.WhenNotNull();
        _validationInvoker = validationInvoker.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public override async Task<int> HandleAsync(string configFilename, CancellationToken cancellationToken)
    {
        try
        {
            // Will throw DirectoryNotFoundException if the associated directory cannot be found
            var configDirectory = GetConfigDirectory(configFilename);

            var document = await LoadDependencyProjectDocumentAsync(configFilename, cancellationToken).ConfigureAwait(false);

            // Log the configuration to help with troubleshooting any validation errors.
            document.LogConfiguration(configFilename, _logger);

            // Validate Pre-Generation Command settings.
            var preGenConfigContext = new PreGenerationConfigContext { ConfigDirectory = configDirectory };
            _validationInvoker.AssertValidation(document.PreGeneration, preGenConfigContext);

            // Validate the main diagram generator configuration.
            _generator.ValidateConfiguration(document.DiagramGenerator);

            if (!await RunPreGenerationCommandIfRequiredAsync(document, cancellationToken))
            {
                return StudioCliExitCode.PreGenerationCommandFailed.Value;
            }

            await GenerateDiagramsAsync(document, cancellationToken);

            _logger.LogInformation("Generation complete.");

            return 0;
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
            return StudioCliExitCode.RunCommandFailed.Value;
        }
        catch (RegexParseException exception)
        {
            _logger.LogError("Invalid regular expression: {Message}", exception.Message);
            return StudioCliExitCode.RunCommandFailed.Value;
        }
        catch (ToolNotFoundException exception)
        {
            _logger.LogError("Required diagram tool not found: {Message}", exception.Message);
            return StudioCliExitCode.DiagramToolNotFound.Value;
        }
        catch (DependencyGeneratorException exception)
        {
            _logger.LogError("Diagram generator failed: {Message}", exception.Message);
            return StudioCliExitCode.DiagramGeneratorFailed.Value;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Operation was cancelled.");
            return StudioCliExitCode.RunCommandFailed.Value;
        }
        catch (JsonException exception)
        {
            _logger.LogError("Failed to parse configuration file. Error on line {LineNumber} for Path {Path}.", exception.LineNumber + 1, exception.Path);
            return StudioCliExitCode.CannotLoadConfigFile.Value;
        }
        catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
        {
            _logger.LogError("Could not load file: {Message}", exception.Message);
            return StudioCliExitCode.CannotLoadConfigFile.Value;
        }
    }

    /// <summary>Runs the pre-generation command if enabled. Returns <see langword="false"/> if the command failed and continue-on-failure is disabled.</summary>
    /// <param name="document">The dependency project document.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> if no command was required or it succeeded; <see langword="false"/> if it failed and should abort.</returns>
    private async Task<bool> RunPreGenerationCommandIfRequiredAsync(DependencyProjectDocument document, CancellationToken cancellationToken)
    {
        var preGenConfig = document.PreGeneration;

        if (!preGenConfig.Enabled)
        {
            _logger.LogInformation("Pre-generation command disabled.");
            return true;
        }

        _logger.LogInformation("Running Pre-generation command...");

        // Subscribe to stdout/stderr so the output is captured in the CLI's log output.
        using var stdoutSub = _preGenerationCommandRunner.StdOut.Subscribe(line => _logger.LogInformation("{Line}", line));
        using var stderrSub = _preGenerationCommandRunner.StdErr.Subscribe(line => _logger.LogError("{Line}", line));

        // Validation ensures the command is set
        var preGenResult = await _preGenerationCommandRunner.RunAsync(preGenConfig, cancellationToken);

        if (!preGenResult.Succeeded)
        {
            _logger.LogError(
                "Pre-generation command failed (exit code {ExitCode}, error: {ErrorMessage}).",
                preGenResult.ExitCode,
                preGenResult.ErrorMessage);

            if (!preGenConfig.ContinueOnFailure)
            {
                _logger.LogError(
                    "Pre-generation command failed and continue-on-failure is disabled. Aborting.\n  {ErrorMessage}",
                    preGenResult.ErrorMessage);

                return false;
            }

            _logger.LogWarning(
                "Pre-generation command failed but continue-on-failure is enabled. Proceeding with generation.\n  {ErrorMessage}",
                preGenResult.ErrorMessage);
        }

        return true;
    }

    /// <summary>Initiates diagram generation via the dependency generator.</summary>
    /// <param name="document">The dependency project document containing the generator configuration.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when diagram generation has finished.</returns>
    private Task GenerateDiagramsAsync(DependencyProjectDocument document, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating diagrams...");

        return _generator.CreateDiagramsAsync(document.DiagramGenerator, cancellationToken);
    }
}
