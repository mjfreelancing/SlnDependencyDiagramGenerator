using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Validators;

internal sealed class GeneratorDiagramOptionsFillStyleValidator : ValidatorBase<GeneratorDiagramOptions.FillStyle>
{
    static GeneratorDiagramOptionsFillStyleValidator()
    {
        DisablePropertyNameSplitting();
    }

    public GeneratorDiagramOptionsFillStyleValidator()
    {
        RuleFor(model => model.Fill).IsNotEmpty();
        RuleFor(model => model.Opacity).InclusiveBetween(0.0d, 1.0d);
    }
}