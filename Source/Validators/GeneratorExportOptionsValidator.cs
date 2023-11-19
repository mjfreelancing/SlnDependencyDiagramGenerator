using AllOverIt.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using System.IO;

namespace SlnDependencyDiagramGenerator.Validators
{
    internal sealed class GeneratorExportOptionsValidator : ValidatorBase<GeneratorExportOptions>
    {
        static GeneratorExportOptionsValidator()
        {
            DisablePropertyNameSplitting();
        }

        public GeneratorExportOptionsValidator()
        {
            RuleFor(model => model.RootPath).IsNotEmpty();

            When(model => model.RootPath.IsNotNullOrEmpty(), () =>
            {
                RuleFor(model => model.RootPath)
                    .Must(Directory.Exists)
                    .WithMessage("The root export path was not found.");
            });

            RuleFor(model => model.ImageFormats).NotNull();
        }
    }
}