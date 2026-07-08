using AllOverIt.Validation;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Validators;

/// <summary>Validates <see cref="DependencyGeneratorConfig"/> options.</summary>
internal sealed class DependencyGeneratorConfigValidator : ValidatorBase<DependencyGeneratorConfig>
{
    /// <summary>Initializes static validator configuration.</summary>
    static DependencyGeneratorConfigValidator()
    {
        DisablePropertyNameSplitting();
    }

    /// <summary>Initializes validation rules.</summary>
    public DependencyGeneratorConfigValidator()
    {
        RuleFor(model => model.Solution).NotNull();
        RuleFor(model => model.Solution).SetValidator(new GeneratorSolutionOptionsValidator());

        RuleFor(model => model.Diagram).NotNull();
        RuleFor(model => model.Diagram).SetValidator(new GeneratorDiagramOptionsValidator());

        RuleFor(model => model.Export).NotNull();
        RuleFor(model => model.Export).SetValidator(new GeneratorExportOptionsValidator());
    }
}
