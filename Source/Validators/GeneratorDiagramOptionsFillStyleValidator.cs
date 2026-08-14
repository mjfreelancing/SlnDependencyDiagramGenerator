using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Validators;

/// <summary>Validates <see cref="GeneratorDiagramOptions.FillStyle"/>.</summary>
internal sealed class GeneratorDiagramOptionsFillStyleValidator : ValidatorBase<GeneratorDiagramOptions.FillStyle>
{
    /// <summary>Initializes static validator configuration.</summary>
    static GeneratorDiagramOptionsFillStyleValidator()
    {
        DisablePropertyNameSplitting();
    }

    /// <summary>Initializes validation rules.</summary>
    public GeneratorDiagramOptionsFillStyleValidator()
    {
        RuleFor(model => model.Fill).IsNotEmpty();
        RuleFor(model => model.Opacity).InclusiveBetween(0.0d, 1.0d);
    }
}