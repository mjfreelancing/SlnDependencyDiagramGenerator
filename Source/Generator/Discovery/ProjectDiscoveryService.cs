using SlnDependencyDiagramGenerator.Parser;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator.Discovery;

/// <summary>Provides project discovery and parsing services by coordinating with <see cref="ISolutionParser" />.</summary>
internal sealed class ProjectDiscoveryService : IProjectDiscoveryService
{
    private readonly ISolutionParser _solutionParser;
    private string _cachedDiscoveryKey = string.Empty;
    private FilteredSolutionProjects? _cachedFilteredProjects;

    /// <summary>Initializes a new instance of <see cref="ProjectDiscoveryService"/>.</summary>
    /// <param name="solutionParser">The parser used to discover and parse solution projects.</param>
    public ProjectDiscoveryService(ISolutionParser solutionParser)
    {
        _solutionParser = solutionParser;
    }

    // For use with integration tests.
    internal ProjectDiscoveryService()
        : this(new SolutionParser())
    {
    }

    /// <inheritdoc />
    public async Task<ProjectDiscoveryResult> DiscoverProjectsAsync(string solutionFilePath, string[] regexToInclude,
        string[] regexToExclude, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var filteredProjects = await GetFilteredProjectsAsync(solutionFilePath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        return new ProjectDiscoveryResult
        {
            AllProjectPaths = [.. filteredProjects.AllProjects.Select(project => project.AbsolutePath)],
            IncludedProjectPaths = [.. filteredProjects.IncludedProjects.Select(project => project.AbsolutePath)],
            ExcludedProjectPaths = [.. filteredProjects.ExcludedProjects.Select(project => project.AbsolutePath)],
            ImplicitlyExcludedProjectPaths = [.. filteredProjects.ImplicitlyExcludedProjects.Select(project => project.AbsolutePath)]
        };
    }

    /// <inheritdoc />
    public Task<SolutionProject[]> ParseProjectsAsync(SolutionParseRequest request, CancellationToken cancellationToken)
    {
        return ParseProjectsInternalAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string[]> DiscoverTargetFrameworksAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken)
    {
        var filteredProjects = await GetFilteredProjectsAsync(solutionFilePath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        return await _solutionParser
            .DiscoverTargetFrameworksAsync(filteredProjects.IncludedProjects, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<SolutionProject[]> ParseProjectsInternalAsync(SolutionParseRequest request, CancellationToken cancellationToken)
    {
        var filteredProjects = await GetFilteredProjectsAsync(request.SolutionFilePath, request.RegexToInclude, request.RegexToExclude, cancellationToken)
            .ConfigureAwait(false);

        return _solutionParser.BuildParsedProjects(request, filteredProjects.IncludedProjects);
    }

    private async Task<FilteredSolutionProjects> GetFilteredProjectsAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken)
    {
        var normalizedSolutionPath = Path.GetFullPath(solutionFilePath);
        var discoveryKey = CreateDiscoveryKey(normalizedSolutionPath, regexToInclude, regexToExclude);

        if (_cachedFilteredProjects is not null && string.Equals(_cachedDiscoveryKey, discoveryKey, StringComparison.Ordinal))
        {
            return _cachedFilteredProjects;
        }

        var filteredProjects = await _solutionParser
            .DiscoverProjectsAsync(normalizedSolutionPath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        _cachedDiscoveryKey = discoveryKey;
        _cachedFilteredProjects = filteredProjects;

        return filteredProjects;
    }

    private static string CreateDiscoveryKey(string solutionFilePath, string[] regexToInclude, string[] regexToExclude)
    {
        return string.Join("|", solutionFilePath, string.Join(";", regexToInclude), string.Join(";", regexToExclude));
    }
}
