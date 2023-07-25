using AllOverIt.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using System.IO;

namespace AllOverItDependencyDiagram.Validator
{
    internal sealed class AppOptionsValidator : ValidatorBase<AppOptions>
    {
        static AppOptionsValidator()
        {
            DisablePropertyNameSplitting();
        }

        public AppOptionsValidator()
        {
            RuleFor(model => model.IndividualProjectTransitiveDepth).IsGreaterThanOrEqualTo(0);
            RuleFor(model => model.AllProjectsTransitiveDepth).IsGreaterThanOrEqualTo(0);

            RuleFor(model => model.PackageStyleFill).IsNotEmpty();
            RuleFor(model => model.TransitiveStyleFill).IsNotEmpty();

            RuleFor(model => model.ImageFormats).NotNull();

            RuleFor(model => model.SolutionPath).IsNotEmpty();

            When(model => model.SolutionPath.IsNotNullOrEmpty(), () =>
            {
                RuleFor(model => model.SolutionPath)
                    .Must(Path.Exists)
                    .WithMessage("The solution was not found.");
            });

            RuleFor(model => model.ProjectPathRegex).IsNotEmpty();
            RuleFor(model => model.TargetFramework).IsNotEmpty();

            RuleFor(model => model.ExportPath).IsNotEmpty();

            When(model => model.ExportPath.IsNotNullOrEmpty(), () =>
            {
                RuleFor(model => model.ExportPath)
                    .Must(Directory.Exists)
                    .WithMessage("The export path was not found.");
            });
        }
    }
}