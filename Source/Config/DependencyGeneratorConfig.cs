using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public class DependencyGeneratorConfig : IDependencyGeneratorConfig
    {
        public IReadOnlyCollection<NugetPackageFeed> PackageFeeds { get; set; }
        public IGeneratorProjectOptions Projects { get; set; } = new GeneratorProjectOptions();
        public IGeneratorDiagramOptions Diagram { get; set; } = new GeneratorDiagramOptions();
        public IGeneratorExportOptions Export { get; set; } = new GeneratorExportOptions();
        public string TargetFramework { get; set; }
    }
}