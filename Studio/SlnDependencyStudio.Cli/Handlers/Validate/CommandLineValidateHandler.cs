using AllOverIt.Assertion;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Shared.Config.Extensions;
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
        _projectValidator = projectValidator.WhenNotNull();
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

            // Validate all configuration up front so failures are reported before any command or
            // generation work begins. The command runners themselves do not perform validation.
            _projectValidator.Validate(document, configDirectory);

            _logger.LogInformation("Configuration is valid.");

            return 0;
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
            return StudioCliExitCode.ValidateCommandFailed.Value;
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
}
