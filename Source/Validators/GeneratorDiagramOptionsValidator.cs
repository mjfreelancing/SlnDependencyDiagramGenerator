using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Validators
{
    internal sealed class GeneratorDiagramOptionsValidator : ValidatorBase<GeneratorDiagramOptions>
    {
        static GeneratorDiagramOptionsValidator()
        {
            DisablePropertyNameSplitting();
        }

        public GeneratorDiagramOptionsValidator()
        {
            var fillStyleValidator = new GeneratorDiagramOptionsFillStyleValidator();

            RuleFor(model => model.FrameworkStyle).SetValidator(fillStyleValidator);
            RuleFor(model => model.PackageStyle).SetValidator(fillStyleValidator);
            RuleFor(model => model.TransitiveStyle).SetValidator(fillStyleValidator);
            RuleFor(model => model.GroupName).IsNotEmpty();
            RuleFor(model => model.GroupNamePrefix).IsNotEmpty();
        }
    }
}