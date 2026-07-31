using AllOverIt.Assertion;
using AllOverIt.Validation;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Validators.Contexts;

namespace SlnDependencyStudio.Shared.Services;

/// <inheritdoc cref="IDependencyProjectValidator"/>
internal sealed class DependencyProjectValidator : IDependencyProjectValidator
{
    private readonly IValidationInvoker _validationInvoker;

    /// <summary>Initializes a new instance of <see cref="DependencyProjectValidator"/>.</summary>
    /// <param name="validationInvoker">The validation invoker used to validate each configuration section.</param>
    public DependencyProjectValidator(IValidationInvoker validationInvoker)
    {
        _validationInvoker = validationInvoker.WhenNotNull();
    }

    /// <inheritdoc />
    public void Validate(DependencyProjectDocument document, string configDirectory)
    {
        // All configuration is validated up front so failures are reported before any command or
        // generation work begins. The command runners themselves do not perform validation.
        _validationInvoker.AssertValidation(document.PreGeneration, new PreGenerationConfigContext { ConfigDirectory = configDirectory });
        _validationInvoker.AssertValidation(document.PostGeneration, new PostGenerationConfigContext { ConfigDirectory = configDirectory });
        _validationInvoker.AssertValidation(document.DiagramGenerator);
    }
}
