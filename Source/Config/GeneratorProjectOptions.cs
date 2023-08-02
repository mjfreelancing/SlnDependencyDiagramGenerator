using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Config
{
    /// <summary>Specifies project related options that determine which projects for a given solution
    /// are resolved and the depth of their package dependency graph.</summary>
    public class GeneratorProjectOptions
    {
        /// <summary>The fully-qualified path to the solution file to be parsed.</summary>
        public string SolutionPath { get; init; }

        /// <summary>One or more regex patterns to match solution projects to be processed. To parse
        /// all <c>.csproj</c> files under a specified path, including sub-folders, use a regex such as
        /// <c>"C:\\Dev\\Project\\Source\\.*\.csproj"</c>. Note that the <c>\\</c> shown in this example
        /// are escaped for the regex pattern. Escape each of these again if used in code or configuration.</summary>
        public IReadOnlyCollection<string> RegexToInclude { get; init; }

        /// <summary>Indicates how deep to traverse implicit (transitive) package references when processing an individual project diagram.
        /// Must be 0 or more.</summary>
        public int IndividualTransitiveDepth { get; init; }

        /// <summary>Indicates how deep to traverse implicit (transitive) package references when processing the "all projects" diagram.
        /// Must be 0 or more.</summary>
        public int AllTransitiveDepth { get; init; }
    }
}