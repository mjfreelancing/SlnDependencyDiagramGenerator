using AllOverIt.Extensions;
using AllOverIt.Validation;
using AllOverIt.Validation.Extensions;
using FluentValidation;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Shared.Validators.Contexts;

namespace SlnDependencyStudio.Shared.Validators;

/// <summary>Validates <see cref="PreGenerationConfig"/> options.</summary>
internal sealed class PreGenerationConfigValidator : ValidatorBase<PreGenerationConfig>
{
    /// <summary>Initializes static validator configuration.</summary>
    static PreGenerationConfigValidator()
    {
        DisablePropertyNameSplitting();
    }

    /// <summary>Initializes validation rules.</summary>
    public PreGenerationConfigValidator()
    {
        When(config => config.Enabled, () =>
        {
            RuleFor(config => config.Command).IsNotEmpty();

            When(config => config.WorkingDirectory.IsNotNullOrEmpty(), () =>
            {
                RuleFor(config => config.WorkingDirectory)
                    .Custom((workingDirectory, context) =>
                    {
                        var preGenerationContext = context.GetContextData<PreGenerationConfig, PreGenerationConfigContext>();

                        var resolvedPath = PathUtils.ResolveAsAbsolutePath(workingDirectory, preGenerationContext.ConfigDirectory);

                        if (!Directory.Exists(resolvedPath))
                        {
                            context.AddFailure(
                                nameof(PreGenerationConfig.WorkingDirectory),
                                $"The pre-generation working directory was not found: {resolvedPath}");
                        }
                    });
            });

            When(config => config.Arguments.IsNotNullOrEmpty(), () =>
            {
                // Double quotes are valid — they group tokens into a single argument
                // (e.g. --file "my file.txt") and are stripped by CommandLineUtils.SplitArguments.
                RuleFor(config => config.Arguments)
                    .Must(argumentLine => !argumentLine.Any(character =>
                        character != '"' && Path.GetInvalidPathChars().Contains(character)))
                    .WithMessage($"{nameof(PreGenerationConfig.Arguments)} contains invalid characters.");
            });

            When(config => config.Command.IsNotNullOrEmpty(), () =>
            {
                RuleFor(config => config.Command)
                    .Must(cmd => !cmd.Any(ch => Path.GetInvalidPathChars().Contains(ch)))
                    .WithMessage($"{nameof(PreGenerationConfig.Command)} contains invalid characters.");
            });
        });
    }
}
