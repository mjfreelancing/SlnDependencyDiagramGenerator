using AllOverIt.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SlnDependencyDiagramGenerator.Validators;

/// <summary>Validates <see cref="GeneratorSolutionOptions"/>.</summary>
internal sealed class GeneratorSolutionOptionsValidator : ValidatorBase<GeneratorSolutionOptions>
{
    /// <summary>Initializes static validator configuration.</summary>
    static GeneratorSolutionOptionsValidator()
    {
        DisablePropertyNameSplitting();
    }

    /// <summary>Initializes validation rules.</summary>
    public GeneratorSolutionOptionsValidator()
    {
        RuleFor(model => model.SolutionPath).IsNotEmpty();

        When(model => model.SolutionPath.IsNotNullOrEmpty(), () =>
        {
            RuleFor(model => model.SolutionPath)
                .Must(path =>
                {
                    var extension = Path.GetExtension(path);

                    return extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) ||
                           extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
                })
                .WithMessage("SolutionPath must reference a .sln or .slnx file.");

            RuleFor(model => model.SolutionPath)
                .Must(path =>
                {
                    var solutionFilePath = Path.GetFullPath(path);
                    return Path.Exists(solutionFilePath);
                })
                .WithMessage(model => $"The solution was not found: {Path.GetFullPath(model.SolutionPath)}");
        });

        RuleFor(model => model.RegexToInclude).IsNotEmpty();

        When(model => model.RegexToInclude is { Length: > 0 }, () =>
        {
            RuleForEach(model => model.RegexToInclude)
                .Must(pattern => ValidateRegexPattern(pattern))
                .WithMessage("'{PropertyValue}' is not a valid regex pattern.");
        });

        RuleFor(model => model.RegexToExclude).NotNull();

        When(model => model.RegexToExclude is { Length: > 0 }, () =>
        {
            RuleForEach(model => model.RegexToExclude)
                .Must(pattern => ValidateRegexPattern(pattern))
                .WithMessage("'{PropertyValue}' is not a valid regex pattern.");
        });

        RuleFor(model => model.PackagesToExclude).NotNull();
        RuleFor(model => model.FrameworksToExclude).NotNull();
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

    private static bool ValidateRegexPattern(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}