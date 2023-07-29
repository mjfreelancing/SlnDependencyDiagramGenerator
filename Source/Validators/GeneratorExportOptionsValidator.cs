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
            RuleFor(model => model.Path).IsNotEmpty();

            When(model => model.Path.IsNotNullOrEmpty(), () =>
            {
                RuleFor(model => model.Path)
                    .Must(Directory.Exists)
                    .WithMessage("The export path was not found.");
            });

            RuleFor(model => model.ImageFormats).NotNull();
        }
    }
}