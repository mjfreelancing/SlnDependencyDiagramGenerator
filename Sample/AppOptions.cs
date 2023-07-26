using System.Collections.Generic;
using AllOverItDependencyDiagram.Generator;

namespace AllOverItDependencyDiagram
{
    public sealed class AppOptions : IProjectDependencyGeneratorOptions
    {
        public int IndividualProjectTransitiveDepth { get; set; } = 1;

        public int AllProjectsTransitiveDepth { get; set; } = 0;

        public string PackageStyleFill { get; set; } = "#ADD8E6";

        public string TransitiveStyleFill { get; set; } = "#FFEC96";

        // Cannot initialize here with a default since anything read from the config is appended
        public IReadOnlyCollection<DiagramImageFormat> ImageFormats { get; set; }

        // Such as C:\Dev\MyApp\MySolution.sln
        public string SolutionPath { get; set; }

        // Such as C:\\Dev\\MyApp\\Source\\.*\.csproj
        public string ProjectPathRegex { get; set; }

        // Such as net7.0
        public string TargetFramework { get; set; }

        public bool ClearExportPath { get; set; }

        public string ExportPath { get; set; }
    }
}