using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;
using SlnDependencyStudio.Shared.ProcessExecution.RestoreSolution;

namespace SlnDependencyStudio.Shared.Tests.Integration.Support;

/// <summary>Helper methods for integration testing pipeline command scenarios.</summary>
internal static class IntegrationTestHarness
{
    /// <summary>Creates a fully wired <see cref="IPreGenerationCommandRunner"/> via DI.</summary>
    public static IPreGenerationCommandRunner CreateRunner()
    {
        var provider = CreateProvider();

        return provider.GetRequiredService<IPreGenerationCommandRunner>();
    }

    /// <summary>Creates a fully wired <see cref="IRestoreSolutionRunner"/> via DI.</summary>
    public static IRestoreSolutionRunner CreateRestoreRunner()
    {
        var provider = CreateProvider();

        return provider.GetRequiredService<IRestoreSolutionRunner>();
    }

    /// <summary>Creates a fully wired <see cref="IPostGenerationCommandRunner"/> via DI.</summary>
    public static IPostGenerationCommandRunner CreatePostGenerationRunner()
    {
        var provider = CreateProvider();

        return provider.GetRequiredService<IPostGenerationCommandRunner>();
    }

    /// <summary>Creates a <see cref="PreGenerationConfig"/> with the specified values.</summary>
    public static PreGenerationConfig CreateConfig(bool enabled, string command, string arguments,
        bool continueOnFailure = false, string? workingDirectory = null)
    {
        return new PreGenerationConfig
        {
            Enabled = enabled,
            Command = command,
            Arguments = arguments,
            ContinueOnFailure = continueOnFailure,
            WorkingDirectory = workingDirectory ?? string.Empty
        };
    }

    /// <summary>Creates a <see cref="PostGenerationConfig"/> with the specified values.</summary>
    public static PostGenerationConfig CreatePostGenerationConfig(bool enabled, string command, string arguments,
        string? workingDirectory = null)
    {
        return new PostGenerationConfig
        {
            Enabled = enabled,
            Command = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? string.Empty
        };
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        var (_, validationRegistry) = services.AddSlnDependencyDiagramGenerator();
        services.AddSlnDependencyStudio(validationRegistry);

        return services.BuildServiceProvider();
    }
}
