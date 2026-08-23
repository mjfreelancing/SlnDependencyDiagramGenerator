using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Represents project classification results after applying include and exclude regex filters.</summary>
internal sealed class FilteredSolutionProjects
{
    /// <summary>All projects discovered from the solution.</summary>
    public required SolutionProjectDescriptor[] AllProjects { get; init; }

    /// <summary>Projects that matched include filters and were not excluded.</summary>
    public required SolutionProjectDescriptor[] IncludedProjects { get; init; }

    /// <summary>Projects that matched include filters and were then removed by exclude filters.</summary>
    public required SolutionProjectDescriptor[] ExcludedProjects { get; init; }

    /// <summary>Projects that did not match any include filter.</summary>
    public required SolutionProjectDescriptor[] ImplicitlyExcludedProjects { get; init; }
}
