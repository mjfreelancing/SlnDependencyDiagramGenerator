using AllOverIt.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using System.IO;

namespace SlnDependencyDiagramGenerator.Validators
{
    internal sealed class GeneratorProjectOptionsValidator : ValidatorBase<GeneratorProjectOptions>
    {
        static GeneratorProjectOptionsValidator()
        {
            DisablePropertyNameSplitting();
        }

        public GeneratorProjectOptionsValidator()
        {
            RuleFor(model => model.SolutionPath).IsNotEmpty();

            When(model => model.SolutionPath.IsNotNullOrEmpty(), () =>
            {
                RuleFor(model => model.SolutionPath)
                    .Must(Path.Exists)
                    .WithMessage("The solution was not found.");
            });

            RuleFor(model => model.RegexToInclude).IsNotEmpty();
            RuleFor(model => model.IndividualTransitiveDepth).IsGreaterThanOrEqualTo(0);
            RuleFor(model => model.AllTransitiveDepth).IsGreaterThanOrEqualTo(0);
        }
    }
}