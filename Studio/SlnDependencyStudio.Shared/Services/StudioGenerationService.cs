using AllOverIt.Assertion;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using FluentValidation;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator;

namespace SlnDependencyStudio.Shared.Services;

/// <summary>Default implementation of <see cref="IStudioGenerationService"/>.</summary>
internal sealed class StudioGenerationService : IStudioGenerationService
{
    private readonly DependencyGenerator _generator;

    /// <summary>Initializes a new instance of <see cref="StudioGenerationService"/>.</summary>
    /// <param name="generator">The dependency diagram generator.</param>
    public StudioGenerationService(DependencyGenerator generator)
    {
        _generator = generator.WhenNotNull();
    }

    /// <inheritdoc />
    public async Task<GenerationResult> RunAsync(DependencyGeneratorConfig generatorConfig, PreGenerationConfig? preGeneration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var preGenerationRan = false;
        var preGenerationSucceeded = true;

        // Execute pre-generation command if configured and enabled.
        if (preGeneration is { Enabled: true, Command.Length: > 0 })
        {
            preGenerationRan = true;
            preGenerationSucceeded = false;

            try
            {
                var processExecutor = ProcessBuilder
                    .For(preGeneration.Command)
                    .WithNoWindow()
                    .WithArguments(preGeneration.Arguments ?? string.Empty)
                    .WithWorkingDirectory(preGeneration.WorkingDirectory ?? string.Empty)
                    .BuildProcessExecutor();

                var processResult = await processExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                preGenerationSucceeded = processResult.ExitCode == 0;

                if (!preGenerationSucceeded && !preGeneration.ContinueOnFailure)
                {
                    return new GenerationResult
                    {
                        Success = false,
                        PreGenerationRan = true,
                        PreGenerationSucceeded = false,
                        ErrorMessage = $"Pre-generation command failed with exit code {processResult.ExitCode}."
                    };
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (!preGeneration.ContinueOnFailure)
                {
                    return new GenerationResult
                    {
                        Success = false,
                        PreGenerationRan = true,
                        PreGenerationSucceeded = false,
                        ErrorMessage = $"Pre-generation command failed: {ex.Message}"
                    };
                }
            }
        }

        // Validate and run diagram generation.
        try
        {
            DependencyGenerator.ValidateConfiguration(generatorConfig);
            await _generator.CreateDiagramsAsync(generatorConfig, cancellationToken).ConfigureAwait(false);

            return new GenerationResult
            {
                Success = true,
                PreGenerationRan = preGenerationRan,
                PreGenerationSucceeded = preGenerationSucceeded
            };
        }
        catch (ValidationException ex)
        {
            return new GenerationResult
            {
                Success = false,
                PreGenerationRan = preGenerationRan,
                PreGenerationSucceeded = preGenerationSucceeded,
                ErrorMessage = string.Join(Environment.NewLine, ex.Errors.Select(error => error.ErrorMessage))
            };
        }
    }
}