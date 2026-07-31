using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Shared.ProcessExecution.PreGeneration;

namespace SlnDependencyStudio.Shared.Tests.Integration.Support;

/// <summary>Helper methods for integration testing pre-generation command scenarios.</summary>
internal static class IntegrationTestHarness
{
    /// <summary>Creates a fully wired <see cref="IPreGenerationCommandRunner"/> via DI.</summary>
    public static IPreGenerationCommandRunner CreateRunner()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        var (_, validationRegistry) = services.AddSlnDependencyGenerator();
        services.AddSlnDependencyStudio(validationRegistry);

        var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IPreGenerationCommandRunner>();
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
}
