using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Shared.Serialization;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Extensions;
using SlnDependencyStudio.Wpf.Features.Application;
using System.IO;

namespace SlnDependencyStudio.Wpf.Tests.Integration.Support;

/// <summary>Helper methods for WPF integration testing.</summary>
internal static class IntegrationTestHarness
{
    /// <summary>Isolated scratch directory for settings integration tests. Tests must never touch the real
    /// <c>%AppData%/SlnDependencyStudio</c> settings/state files.</summary>
    internal static readonly string SettingsDirectory = Path.Combine(
        Path.GetTempPath(), "SlnDependencyStudio.Tests.Integration", Guid.NewGuid().ToString("N"));

    /// <summary>Creates a service provider with all WPF dependencies registered.</summary>
    public static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        var (_, validationRegistry) = services.AddSlnDependencyGenerator();
        services.AddSlnDependencyStudio(validationRegistry);
        services.AddWpfDependencies();

        // Point the settings service at the isolated scratch directory instead of the user's real profile.
        services.AddSingleton<IApplicationSettingsService>(provider =>
            new ApplicationSettingsService(
                provider.GetRequiredService<IStudioJsonSerializer>(),
                provider.GetRequiredService<IFileSystem>(),
                SettingsDirectory,
                provider.GetRequiredService<ILogger<ApplicationSettingsService>>()));

        return services.BuildServiceProvider();
    }

    /// <summary>Creates a <see cref="DependencyProjectDocument"/> with the specified values.</summary>
    public static DependencyProjectDocument CreateDocument(
        string projectName = "Test Project",
        string? solutionPath = null,
        DiagramFormat format = DiagramFormat.Mermaid,
        GeneratorDiagramOptions.DiagramDirection direction = GeneratorDiagramOptions.DiagramDirection.LR)
    {
        return new DependencyProjectDocument
        {
            Metadata = new DependencyProjectMetadata
            {
                ProjectName = projectName,
                Description = "Integration test project"
            },
            DiagramGenerator = new DependencyGeneratorConfig
            {
                Solution = new GeneratorSolutionOptions
                {
                    SolutionPath = solutionPath ?? Path.Combine(AppContext.BaseDirectory, "Fixtures", "TestSolution.sln")
                },
                Diagram = new GeneratorDiagramOptions
                {
                    Formats = [format],
                    Direction = direction
                },
                Export = new GeneratorExportOptions
                {
                    RootPath = Path.Combine(Path.GetTempPath(), "SlnDependencyGenTest")
                }
            }
        };
    }
}
