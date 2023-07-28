using AllOverIt.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using System.IO;

namespace SlnDependencyDiagramGenerator.Validators
{
    internal sealed class DependencyGeneratorConfigValidator : ValidatorBase<DependencyGeneratorConfig>
    {
        static DependencyGeneratorConfigValidator()
        {
            DisablePropertyNameSplitting();
        }

        public DependencyGeneratorConfigValidator()
        {
            RuleFor(model => model.PackageFeeds).IsNotEmpty();

            When(model => model.PackageFeeds.IsNotNullOrEmpty(), () =>
            {
                RuleForEach(model => model.PackageFeeds).SetValidator(new PackageFeedValidator());
            });

            RuleFor(model => model.Projects.SolutionPath).IsNotEmpty();

            When(model => model.Projects.SolutionPath.IsNotNullOrEmpty(), () =>
            {
                RuleFor(model => model.Projects.SolutionPath)
                    .Must(Path.Exists)
                    .WithMessage("The solution was not found.");
            });

            RuleFor(model => model.Projects.RegexToInclude).IsNotEmpty();
            RuleFor(model => model.Projects.IndividualTransitiveDepth).IsGreaterThanOrEqualTo(0);
            RuleFor(model => model.Projects.AllTransitiveDepth).IsGreaterThanOrEqualTo(0);

            RuleFor(model => model.Diagram.PackageFill).IsNotEmpty();
            RuleFor(model => model.Diagram.TransitiveFill).IsNotEmpty();
            RuleFor(model => model.Diagram.GroupName).IsNotEmpty();
            RuleFor(model => model.Diagram.GroupNamePrefix).IsNotEmpty();

            RuleFor(model => model.TargetFramework).IsNotEmpty();

            RuleFor(model => model.Export.Path).IsNotEmpty();

            When(model => model.Export.Path.IsNotNullOrEmpty(), () =>
            {
                RuleFor(model => model.Export.Path)
                    .Must(Directory.Exists)
                    .WithMessage("The export path was not found.");
            });

            RuleFor(model => model.Export.ImageFormats).NotNull();
        }
    }
}