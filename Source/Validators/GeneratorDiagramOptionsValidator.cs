using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Validators;

/// <summary>Validates <see cref="GeneratorDiagramOptions"/>.</summary>
internal sealed class GeneratorDiagramOptionsValidator : ValidatorBase<GeneratorDiagramOptions>
{
    /// <summary>Initializes static validator configuration.</summary>
    static GeneratorDiagramOptionsValidator()
    {
        DisablePropertyNameSplitting();
    }

    /// <summary>Initializes validation rules.</summary>
    public GeneratorDiagramOptionsValidator()
    {
        var fillStyleValidator = new GeneratorDiagramOptionsFillStyleValidator();

        RuleFor(model => model.FrameworkStyle).NotNull();
        RuleFor(model => model.FrameworkStyle).SetValidator(fillStyleValidator);

        RuleFor(model => model.PackageStyle).NotNull();
        RuleFor(model => model.PackageStyle).SetValidator(fillStyleValidator);

        RuleFor(model => model.TransitiveStyle).NotNull();
        RuleFor(model => model.TransitiveStyle).SetValidator(fillStyleValidator);

        RuleFor(model => model.Direction).IsInEnum();
        RuleFor(model => model.GroupName).IsNotEmpty();
        RuleFor(model => model.GroupNameAlias).IsNotEmpty();
        RuleFor(model => model.Grouping).NotNull();

        When(model => model.Grouping is not null, () =>
        {
            RuleFor(model => model.Grouping.BackgroundStyle).NotNull();
            RuleFor(model => model.Grouping.BackgroundStyle).SetValidator(fillStyleValidator);
        });

        RuleFor(model => model.Formats).NotNull();
        RuleFor(model => model.Formats).IsNotEmpty();
        RuleForEach(model => model.Formats).IsInEnum();
    }
}