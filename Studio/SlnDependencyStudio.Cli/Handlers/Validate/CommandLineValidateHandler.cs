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

internal sealed class CommandLineValidateHandler : CommandLineHandlerBase, ICommandLineValidateHandler
{
    private readonly IDependencyGenerator _generator;
    private readonly IValidationInvoker _validationInvoker;
    private readonly ILogger<CommandLineValidateHandler> _logger;

    public CommandLineValidateHandler(IDependencyProjectSerializer serializer, IDependencyGenerator generator,
        IValidationInvoker validationInvoker, ILogger<CommandLineValidateHandler> logger)
        : base(serializer, logger)
    {
        _generator = generator.WhenNotNull();
        _validationInvoker = validationInvoker.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

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
        catch (Exception exception) when (exception is DirectoryNotFoundException or FileNotFoundException)
        {
            _logger.LogError("Could not load file: {Message}", exception.Message);
            return StudioCliExitCode.ConfigFileNotFound.Value;
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
            return StudioCliExitCode.ValidateCommandFailed.Value;
        }
    }
}
