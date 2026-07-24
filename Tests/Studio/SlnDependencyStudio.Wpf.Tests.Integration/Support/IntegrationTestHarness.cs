using Microsoft.Extensions.DependencyInjection;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Extensions;
using SlnDependencyStudio.Wpf.Extensions;
using System.IO;

namespace SlnDependencyStudio.Wpf.Tests.Integration.Support;

/// <summary>Helper methods for WPF integration testing.</summary>
internal static class IntegrationTestHarness
{
    /// <summary>Creates a service provider with all WPF dependencies registered.</summary>
    public static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        var (_, validationRegistry) = services.AddSlnDependencyGenerator();
        services.AddSlnDependencyStudio(validationRegistry);
        services.AddWpfDependencies();

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
