using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Validators;

/// <summary>Validates <see cref="GeneratorExportOptions"/>.</summary>
internal sealed class GeneratorExportOptionsValidator : ValidatorBase<GeneratorExportOptions>
{
    /// <summary>Initializes static validator configuration.</summary>
    static GeneratorExportOptionsValidator()
    {
        DisablePropertyNameSplitting();
    }

    /// <summary>Initializes validation rules.</summary>
    public GeneratorExportOptionsValidator()
    {
        RuleFor(model => model.RootPath).IsNotEmpty();
        RuleFor(model => model.ImageFormats).NotNull();
        RuleForEach(model => model.ImageFormats).IsInEnum();
    }
}