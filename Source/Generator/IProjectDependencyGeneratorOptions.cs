using System.Collections.Generic;

namespace AllOverItDependencyDiagram.Generator
{
    public interface IProjectDependencyGeneratorOptions
    {
        // How deep to traverse implicit (transitive) package references when processing an individual project diagram. Must be 0 or more.
        int IndividualProjectTransitiveDepth { get; }

        // How deep to traverse implicit (transitive) package references when processing the "all projects" diagram. Must be 0 or more.
        int AllProjectsTransitiveDepth { get; }

        // RGB fill color for explicit package references.
        string PackageStyleFill { get; }

        // RGB fill color for implicit (transitive) package references.
        string TransitiveStyleFill { get; }

        // Diagram image formats to create. Can be empty or one or more of "png", "svg", "pdf".
        IReadOnlyCollection<DiagramImageFormat> ImageFormats { get; }

        // The fully-qualified path to the solution file to be parsed.
        // Such as C:\Dev\MyApp\MySolution.sln
        string SolutionPath { get; }

        // The regex pattern to match solution projects to be processed.
        // Such as C:\\Dev\\MyApp\\Source\\.*\\.csproj
        string ProjectPathRegex { get; }

        // The target framework to resolve implicit (transitive) packages.
        string TargetFramework { get; }

        // The fully-qualified export path for the generated diagram files and images.
        string ExportPath { get; }
    }
}