using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Shared.Config.Extensions;
using SlnDependencyStudio.Shared.Exceptions;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Services;
using System.Text.Json;

namespace SlnDependencyStudio.Cli.Handlers.Validate;

/// <inheritdoc cref="ICommandLineValidateHandler"/>
internal sealed class CommandLineValidateHandler : CommandLineHandlerBase, ICommandLineValidateHandler
{
    private readonly IDependencyProjectValidator _projectValidator;
    private readonly ILogger<CommandLineValidateHandler> _logger;

    /// <summary>Initializes a new instance of <see cref="CommandLineValidateHandler"/>.</summary>
    /// <param name="serializer">The dependency project document serializer.</param>
    /// <param name="projectValidator">The dependency project document validator.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandLineValidateHandler(IDependencyProjectSerializer serializer,
        IDependencyProjectValidator projectValidator, ILogger<CommandLineValidateHandler> logger)
        : base(serializer, logger)
    {
        _projectValidator = projectValidator;
        _logger = logger;
    }

    // Cancellation here relies on OperationCanceledException (OCE) only, deliberately: the validate pipeline contains
    // no process-command step, so nothing reports cancellation as a failed result - the sole cancellable step
    // (document deserialization) observes the token and throws OCE, which is caught (logged with the stage for traceability)
    // and rethrown so App owns the exit-code mapping. Unlike CommandLineRunHandler, there is no subprocess at stake in the tail,
    // so no post-step IsCancellationRequested check is needed.
    /// <inheritdoc />
    public override async Task<int> HandleAsync(string projectFilename, CancellationToken cancellationToken)
    {
        try
        {
            // Will throw DirectoryNotFoundException if the associated directory cannot be found
            var projectDirectory = GetProjectDirectory(projectFilename);

            var document = await LoadDependencyProjectDocumentAsync(projectFilename, cancellationToken).ConfigureAwait(false);

            // Log the configuration to help with troubleshooting any validation errors.
            document.LogConfiguration(projectFilename, _logger);

            // Validate all configuration up front so failures are reported before any command or
            // generation work begins. The command runners themselves do not perform validation.
            _projectValidator.Validate(document, projectDirectory);

            _logger.LogInformation("Configuration is valid.");
            return 0;
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
            return (int)StudioCliExitCode.ValidateCommandFailed;
        }
        catch (OperationCanceledException)
        {
            // Log the cancellation here so the log records where it originated (which stage was in
            // flight); the exit code is assigned by App, which distinguishes a user-requested shutdown
            // from an internal operation cancellation. Rethrow so the code mapping stays in one place.
            _logger.LogWarning("Operation was cancelled during validation.");
            throw;
        }
        catch (DependencyProjectException exception)
        {
            _logger.LogError("The project could not be loaded: {Message}", exception.Message);
            return (int)StudioCliExitCode.CannotLoadProjectFile;
        }
        catch (JsonException exception)
        {
            _logger.LogError("Failed to parse the project file. Error on line {LineNumber} for Path {Path}.", exception.LineNumber + 1, exception.Path);
            return (int)StudioCliExitCode.CannotLoadProjectFile;
        }
        catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
        {
            _logger.LogError("Could not load file: {Message}", exception.Message);
            return (int)StudioCliExitCode.CannotLoadProjectFile;
        }
    }
}
