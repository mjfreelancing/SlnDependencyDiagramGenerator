using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Shared.Services;

/// <summary>Shared generation orchestration consumed by CLI and WPF frontends.
/// Handles pre-generation command execution, config validation, and diagram generation.</summary>
public interface IStudioGenerationService : IStudioScopedDependency
{
    /// <summary>Runs pre-generation (if configured) and diagram generation.</summary>
    /// <param name="generatorConfig">The generator configuration.</param>
    /// <param name="preGeneration">Optional pre-generation command configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="GenerationResult"/> describing the outcome.</returns>
    Task<GenerationResult> RunAsync(DependencyGeneratorConfig generatorConfig, PreGenerationConfig? preGeneration,
        CancellationToken cancellationToken);
}