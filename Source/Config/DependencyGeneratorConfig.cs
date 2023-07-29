using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public class DependencyGeneratorConfig
    {
        public IReadOnlyCollection<NugetPackageFeed> PackageFeeds { get; init; }
        public GeneratorProjectOptions Projects { get; init; } = new GeneratorProjectOptions();
        public GeneratorDiagramOptions Diagram { get; init; } = new GeneratorDiagramOptions();
        public GeneratorExportOptions Export { get; init; } = new GeneratorExportOptions();
        public string TargetFramework { get; init; }
    }
}