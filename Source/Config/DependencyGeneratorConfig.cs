using System.Collections.Generic;
using System.Runtime.Serialization;
using AllOverItDependencyDiagram.Generator;

namespace SlnDependencyDiagramGenerator.Config
{
    public class DependencyGeneratorConfig
    {
        public class ProjectOptions
        {
            // The fully-qualified path to the solution file to be parsed.
            // Such as C:\Dev\MyApp\MySolution.sln
            public string SolutionPath { get; set; }

            // One or more regex patterns to match solution projects to be processed.
            // Such as C:\\Dev\\MyApp\\Source\\.*\\.csproj
            public IReadOnlyCollection<string> RegexToInclude { get; set; }

            // How deep to traverse implicit (transitive) package references when processing an individual project diagram. Must be 0 or more.
            public int IndividualTransitiveDepth { get; set; }

            // How deep to traverse implicit (transitive) package references when processing the "all projects" diagram. Must be 0 or more.
            public int AllTransitiveDepth { get; set; }
        }

        public class StyleOptions
        {
            // RGB fill color for explicit package references.
            public string PackageFill { get; set; }

            // RGB fill color for implicit (transitive) package references.
            public string TransitiveFill { get; set; }
        }

        public class ExportOptions
        {
            // Clears the contents of the 'Path' when true
            public bool ClearContents { get; set; }

            // The fully-qualified export path for the generated diagram files and images.
            public string Path { get; set; }

            // Diagram image formats to create. Can be empty or one or more of "png", "svg", "pdf".
            public IReadOnlyCollection<DiagramImageFormat> ImageFormats { get; set; }
        }

        // One or more Nuget package feeds with auth (if required).
        public IReadOnlyCollection<NugetPackageFeed> PackageFeeds { get; set; }

        public ProjectOptions Projects { get; set; } = new();

        public StyleOptions Style { get; set; }

        public ExportOptions Export { get; set; }

        // The target framework, such as net7.0, to resolve implicit (transitive) packages.
        public string TargetFramework { get; set; }
    }
}