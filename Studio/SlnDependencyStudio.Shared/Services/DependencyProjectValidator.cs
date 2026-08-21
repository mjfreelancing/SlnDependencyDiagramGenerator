using AllOverIt.Assertion;
using AllOverIt.Validation;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Validators.Contexts;

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
    public void Validate(DependencyProjectDocument document, string projectDirectory)
    {
        _ = document.WhenNotNull();
        _ = projectDirectory.WhenNotNull();

        // All configuration is validated up front so failures are reported before any command or
        // generation work begins. The command runners themselves do not perform validation.

        // Pre/post-generation working directories may be relative to the project file, so the
        // project directory is supplied as validation context to resolve and verify them.
        Validate(
            "Pre-generation",
            document.PreGeneration,
            new PreGenerationConfigContext { ProjectDirectory = projectDirectory });

        Validate("Diagram generator", document.DiagramGenerator);

        Validate(
            "Post-generation",
            document.PostGeneration,
            new PostGenerationConfigContext { ProjectDirectory = projectDirectory });
    }

    private void Validate<TConfig>(string configType, TConfig config)
    {
        _logger.LogDebug("Validating {ConfigType} configuration", configType);

        _validationInvoker.AssertValidation(config);

        _logger.LogDebug("{ConfigType} configuration is valid", configType);
    }

    private void Validate<TConfig, TContext>(string configType, TConfig config, TContext context)
    {
        _logger.LogDebug("Validating {ConfigType} configuration", configType);

        _validationInvoker.AssertValidation(config, context);

        _logger.LogDebug("{ConfigType} configuration is valid", configType);
    }
}
