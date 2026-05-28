using AllOverIt.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using System.IO;

namespace SlnDependencyDiagramGenerator.Validators;

/// <summary>Validates <see cref="GeneratorProjectOptions"/>.</summary>
internal sealed class GeneratorProjectOptionsValidator : ValidatorBase<GeneratorProjectOptions>
{
    /// <summary>Initializes static validator configuration.</summary>
    static GeneratorProjectOptionsValidator()
    {
        DisablePropertyNameSplitting();
    }

    /// <summary>Initializes validation rules.</summary>
    public GeneratorProjectOptionsValidator()
    {
        RuleFor(model => model.SolutionPath).IsNotEmpty();

        When(model => model.SolutionPath.IsNotNullOrEmpty(), () =>
        {
            RuleFor(model => model.SolutionPath)
                .Must(path =>
                {
                    var solutionFilePath = Path.GetFullPath(path);
                    return Path.Exists(solutionFilePath);
                })
                .WithMessage(model => $"The solution was not found: {Path.GetFullPath(model.SolutionPath)}");
        });

        RuleFor(model => model.RegexToInclude).NotNull();
        RuleFor(model => model.RegexToInclude).IsNotEmpty();
        RuleFor(model => model.RegexToExclude).NotNull();
        RuleFor(model => model.PackagesToExclude).NotNull();

        RuleFor(model => model.Individual).NotNull();
        RuleFor(model => model.All).NotNull();

        When(model => model.Individual is not null, () =>
        {
            RuleFor(model => model.Individual.TransitiveDepth).IsGreaterThanOrEqualTo(0);
        });

        When(model => model.All is not null, () =>
        {
            RuleFor(model => model.All.TransitiveDepth).IsGreaterThanOrEqualTo(0);
        });
    }
}