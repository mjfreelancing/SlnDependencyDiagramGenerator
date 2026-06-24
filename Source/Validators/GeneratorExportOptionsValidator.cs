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

        // Can't validate since the document needs to be loaded first - which would raise a JsonException if the value is invalid.
        // RuleForEach(model => model.ImageFormats).IsInEnum();
    }
}