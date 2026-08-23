namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Request parameters for <see cref="SolutionParser.ParseAsync"/>.</summary>
public sealed class SolutionParseRequest
{
    /// <summary>The solution path.</summary>
    public required string SolutionFilePath { get; init; }

    /// <summary>Regex patterns used to include projects.</summary>
    public required string[] RegexToInclude { get; init; }

    /// <summary>Regex patterns used to exclude projects.</summary>
    public required string[] RegexToExclude { get; init; }

    /// <summary>Package IDs to exclude from package resolution.</summary>
    public required string[] ExcludePackages { get; init; }

    /// <summary>Framework reference IDs to exclude from framework resolution.</summary>
    public required string[] ExcludeFrameworks { get; init; }

    /// <summary>The target framework to parse.</summary>
    public required string TargetFramework { get; init; }

    /// <summary>The maximum transitive package depth to include.</summary>
    public required int MaxTransitiveDepth { get; init; }
}
