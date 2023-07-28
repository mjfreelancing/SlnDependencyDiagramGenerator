using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    public interface IGeneratorProjectOptions
    {
        // The fully-qualified path to the solution file to be parsed.
        // Such as C:\Dev\MyApp\MySolution.sln
        string SolutionPath { get; }

        // One or more regex patterns to match solution projects to be processed.
        // Such as C:\\Dev\\MyApp\\Source\\.*\\.csproj
        IReadOnlyCollection<string> RegexToInclude { get; }

        // How deep to traverse implicit (transitive) package references when processing an individual project diagram. Must be 0 or more.
        int IndividualTransitiveDepth { get; }

        // How deep to traverse implicit (transitive) package references when processing the "all projects" diagram. Must be 0 or more.
        int AllTransitiveDepth { get; }
    }
}