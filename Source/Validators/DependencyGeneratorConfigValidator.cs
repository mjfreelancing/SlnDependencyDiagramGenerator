using AllOverIt.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using SlnDependencyDiagramGenerator.Config;

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


            RuleFor(model => model.Projects).SetValidator(new GeneratorProjectOptionsValidator());
            RuleFor(model => model.Diagram).SetValidator(new GeneratorDiagramOptionsValidator());
            RuleFor(model => model.TargetFramework).IsNotEmpty();
            RuleFor(model => model.Export).SetValidator(new GeneratorExportOptionsValidator());
        }
    }
}