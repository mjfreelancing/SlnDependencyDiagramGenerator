using AllOverIt.Validation;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;

namespace SlnDependencyStudio.Shared.Services;

/// <inheritdoc cref="IDependencyProjectValidator"/>
internal sealed class DependencyProjectValidator : IDependencyProjectValidator
{
    private readonly IValidationInvoker _validationInvoker;
    private readonly ILogger<DependencyProjectValidator> _logger;

    /// <summary>Initializes a new instance of <see cref="DependencyProjectValidator"/>.</summary>
    /// <param name="validationInvoker">The validation invoker used to validate each configuration section.</param>
    /// <param name="logger">The logger instance.</param>
    public DependencyProjectValidator(IValidationInvoker validationInvoker, ILogger<DependencyProjectValidator> logger)
    {
        _validationInvoker = validationInvoker;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Validate(DependencyProjectDocument document, string configDirectory)
    {
        // All configuration is validated up front so failures are reported before any command or
        // generation work begins. The command runners themselves do not perform validation.

        Validate("Pre-generation", document.PreGeneration);
        Validate("Diagram generator", document.DiagramGenerator);
        Validate("Post-generation", document.PostGeneration);
    }

    private void Validate<TConfig>(string configType, TConfig config)
    {
        _logger.LogDebug("Validating {ConfigType} configuration", configType);

        _validationInvoker.AssertValidation(config);

        _logger.LogInformation("{ConfigType} configuration is valid", configType);
    }
}
