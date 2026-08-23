using SlnDependencyDiagramGenerator.Parser;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator.Discovery;

/// <summary>Provides lightweight project discovery and full parse capabilities for a solution.</summary>
public interface IProjectDiscoveryService
{
    /// <summary>Discovers projects in a solution and classifies them as included or excluded based on
    /// the provided regex filters, without performing MSBuild evaluation or package resolution.</summary>
    /// <param name="solutionFilePath">The solution file path.</param>
    /// <param name="regexToInclude">Regex patterns to match included projects.</param>
    /// <param name="regexToExclude">Regex patterns to match excluded projects.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A discovery result with included, excluded, and all project paths.</returns>
    Task<ProjectDiscoveryResult> DiscoverProjectsAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken);

    /// <summary>Fully parses a solution for a specific target framework, including MSBuild evaluation
    /// and package resolution.</summary>
    /// <param name="request">The parse request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved solution projects.</returns>
    Task<SolutionProject[]> ParseProjectsAsync(SolutionParseRequest request, CancellationToken cancellationToken);

    /// <summary>Discovers the set of target frameworks available across the matching projects.</summary>
    /// <param name="solutionFilePath">The solution file path.</param>
    /// <param name="regexToInclude">Regex patterns to match included projects.</param>
    /// <param name="regexToExclude">Regex patterns to match excluded projects.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The distinct target framework monikers found across matching projects.</returns>
    Task<string[]> DiscoverTargetFrameworksAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken);
}