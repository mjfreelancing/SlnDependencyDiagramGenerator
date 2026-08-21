using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Shared.Services;

/// <summary>
/// Validates all configuration sections of a dependency project document.
/// </summary>
public interface IDependencyProjectValidator : IStudioScopedDependency
{
    /// <summary>Validates the pre-generation, post-generation, and diagram generator configuration.</summary>
    /// <param name="document">The dependency project document to validate.</param>
    /// <param name="projectDirectory">The directory used to resolve relative working-directory paths.</param>
    /// <exception cref="FluentValidation.ValidationException">Thrown when any configuration section is invalid.</exception>
    void Validate(DependencyProjectDocument document, string projectDirectory);
}
