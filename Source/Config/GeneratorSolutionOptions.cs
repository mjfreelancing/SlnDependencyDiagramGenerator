namespace SlnDependencyDiagramGenerator.Config;

/// <summary>Specifies solution-related options: which solution file to parse, regex filters,
/// exclusions, and per-scope depth configuration.</summary>
public sealed class GeneratorSolutionOptions
{
    /// <summary>Contains options relevant to several project scope options.</summary>
    public sealed class ProjectScope
    {
        /// <summary>Indicates if this project scope will be processed.</summary>
        public bool Enabled { get; set; }

        /// <summary>Indicates if framework and package dependencies should be processed.</summary>
        public bool IncludeDependencies { get; set; }

        /// <summary>Indicates how deep to traverse implicit (transitive) package references.
        /// Must be 0 or more.</summary>
        public int TransitiveDepth { get; set; }
    }

    /// <summary>The relative or fully-qualified path to the solution file to be parsed.</summary>
    public string SolutionPath { get; set; } = string.Empty;

    /// <summary>One or more regex patterns to match solution projects to be included. To parse
    /// all <c>.csproj</c> files under a specific path, including sub-folders, use a regex such as
    /// <c>"C:\\Dev\\Project\\Source\\.*\.csproj"</c>. Note that the <c>\\</c> shown in this example
    /// are escaped for the regex pattern. Escape each of these again if used in code or configuration.</summary>
    public string[] RegexToInclude { get; set; } = [];

    /// <summary>One or more (optional) regex patterns to match solution projects to be excluded.</summary>
    public string[] RegexToExclude { get; set; } = [];

    /// <summary>One or more (optional) package IDs to exclude from diagram and summary output (case-insensitive).
    /// Transitive dependencies reachable only through excluded packages are also omitted.</summary>
    public string[] PackagesToExclude { get; set; } = [];

    /// <summary>One or more (optional) framework reference IDs to exclude from diagram and summary output (case-insensitive).</summary>
    public string[] FrameworksToExclude { get; set; } = [];

    /// <summary>Specifies options specific to the processing of individual projects in a solution.</summary>
    public ProjectScope Individual { get; set; } = new();

    /// <summary>Specifies options specific to the processing of all projects in the solution (collectively).</summary>
    public ProjectScope All { get; set; } = new();
}