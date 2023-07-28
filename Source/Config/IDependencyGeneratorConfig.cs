using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public interface IDependencyGeneratorConfig
    {
        // One or more Nuget package feeds with auth (if required).
        IReadOnlyCollection<NugetPackageFeed> PackageFeeds { get; }

        IGeneratorProjectOptions Projects { get; }
        IGeneratorExportOptions Export { get; }
        IGeneratorDiagramOptions Diagram { get; }

        // The target framework, such as net7.0, to resolve implicit (transitive) packages.
        string TargetFramework { get; }
    }
}