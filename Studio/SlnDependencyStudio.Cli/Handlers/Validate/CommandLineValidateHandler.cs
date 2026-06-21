using AllOverIt.Assertion;
using AllOverIt.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Shared.Config.Extensions;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Shared.Validators.Contexts;

namespace SlnDependencyStudio.Cli.Handlers.Validate;

/// <inheritdoc cref="ICommandLineValidateHandler"/>
internal sealed class CommandLineValidateHandler : CommandLineHandlerBase, ICommandLineValidateHandler
{
    private readonly IDependencyGenerator _generator;
    private readonly IValidationInvoker _validationInvoker;
    private readonly ILogger<CommandLineValidateHandler> _logger;

    /// <summary>Initializes a new instance of <see cref="CommandLineValidateHandler"/>.</summary>
    /// <param name="serializer">The dependency project document serializer.</param>
    /// <param name="generator">The dependency diagram generator.</param>
    /// <param name="validationInvoker">The validation invoker for model validation.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandLineValidateHandler(IDependencyProjectSerializer serializer, IDependencyGenerator generator,
        IValidationInvoker validationInvoker, ILogger<CommandLineValidateHandler> logger)
        : base(serializer, logger)
    {
        _generator = generator.WhenNotNull();
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

            _logger.LogInformation("Configuration is valid.");

            return 0;
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
            return StudioCliExitCode.ValidateCommandFailed.Value;
        }
        catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
        {
            _logger.LogError("Could not load file: {Message}", exception.Message);
            return StudioCliExitCode.ConfigFileNotFound.Value;
        }
    }
}
