using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Parser;

/// <summary>Abstraction over solution parsing, enabling unit testing of consumers
/// such as <see cref="Generator.Discovery.ProjectDiscoveryService"/>.</summary>
internal interface ISolutionParser
{
    /// <summary>
    /// Discovers and classifies all projects for the supplied regex filters.
    /// </summary>
    /// <param name="solutionFilePath">The solution path.</param>
    /// <param name="regexToInclude">Regex patterns used to include projects.</param>
    /// <param name="regexToExclude">Regex patterns used to exclude projects.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The full filtered classification.</returns>
    Task<FilteredSolutionProjects> DiscoverProjectsAsync(string solutionFilePath, string[] regexToInclude,
        string[] regexToExclude, CancellationToken cancellationToken);

    /// <summary>
    /// Discovers target frameworks for an already filtered set of projects.
    /// </summary>
    /// <param name="projects">The pre-filtered projects.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A distinct, ordered list of base target frameworks.</returns>
    Task<string[]> DiscoverTargetFrameworksAsync(IReadOnlyList<SolutionProjectDescriptor> projects,
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds parsed project models from an already filtered set for the requested target framework.
    /// </summary>
    /// <param name="request">The parse request parameters.</param>
    /// <param name="projects">The pre-filtered projects.</param>
    /// <returns>The built project models for the requested target framework.</returns>
    SolutionProject[] BuildParsedProjects(SolutionParseRequest request, IReadOnlyList<SolutionProjectDescriptor> projects);
}
