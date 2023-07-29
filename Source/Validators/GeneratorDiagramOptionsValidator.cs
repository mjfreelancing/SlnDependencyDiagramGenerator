using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Validators
{
    internal sealed class GeneratorDiagramOptionsValidator : ValidatorBase<IGeneratorDiagramOptions>
    {
        static GeneratorDiagramOptionsValidator()
        {
            DisablePropertyNameSplitting();
        }

        public GeneratorDiagramOptionsValidator()
        {
            RuleFor(model => model.PackageFill).IsNotEmpty();
            RuleFor(model => model.TransitiveFill).IsNotEmpty();
            RuleFor(model => model.GroupName).IsNotEmpty();
            RuleFor(model => model.GroupNamePrefix).IsNotEmpty();
        }
    }
}