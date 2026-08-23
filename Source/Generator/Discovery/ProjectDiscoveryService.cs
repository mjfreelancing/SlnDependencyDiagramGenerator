using AllOverIt.Caching;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Utils;
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
    private readonly ILogger<ProjectDiscoveryService> _logger;
    private GenericCacheKey<string, long, EquatableArray<string>, EquatableArray<string>>? _cachedDiscoveryKey;
    private FilteredSolutionProjects? _cachedFilteredProjects;

    /// <summary>Initializes a new instance of <see cref="ProjectDiscoveryService"/>.</summary>
    /// <param name="solutionParser">The parser used to discover and parse solution projects.</param>
    /// <param name="logger">The logger used for diagnostics.</param>
    public ProjectDiscoveryService(ISolutionParser solutionParser, ILogger<ProjectDiscoveryService> logger)
    {
        _solutionParser = solutionParser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ProjectDiscoveryResult> DiscoverProjectsAsync(string solutionFilePath, string[] regexToInclude,
        string[] regexToExclude, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Discovering projects for {SolutionFilePath}, Include Regex: {IncludeRegex}, Exclude Regex: {ExcludeRegex}",
            solutionFilePath, string.Join("; ", regexToInclude), string.Join("; ", regexToExclude));

        var filteredProjects = await GetFilteredProjectsAsync(solutionFilePath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        var result = new ProjectDiscoveryResult
        {
            AllProjectPaths = [.. filteredProjects.AllProjects.Select(project => project.AbsolutePath)],
            IncludedProjectPaths = [.. filteredProjects.IncludedProjects.Select(project => project.AbsolutePath)],
            ExcludedProjectPaths = [.. filteredProjects.ExcludedProjects.Select(project => project.AbsolutePath)],
            ImplicitlyExcludedProjectPaths = [.. filteredProjects.ImplicitlyExcludedProjects.Select(project => project.AbsolutePath)]
        };

        _logger.LogDebug(
            "Project discovery complete: {IncludedCount} included, {ExcludedCount} excluded, {ImplicitlyExcludedCount} implicitly excluded of {TotalCount} project(s)",
            result.IncludedProjectPaths.Length,
            result.ExcludedProjectPaths.Length,
            result.ImplicitlyExcludedProjectPaths.Length,
            result.AllProjectPaths.Length);

        return result;
    }

    /// <inheritdoc />
    public async Task<SolutionProject[]> ParseProjectsAsync(SolutionParseRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Getting filtered projects for {SolutionFilePath}, Include Regex: {IncludeRegex}, Exclude Regex: {ExcludeRegex}",
            request.SolutionFilePath, request.RegexToInclude, request.RegexToExclude);

        var filteredProjects = await GetFilteredProjectsAsync(request.SolutionFilePath, request.RegexToInclude, request.RegexToExclude, cancellationToken)
            .ConfigureAwait(false);

        var parsedProjects = _solutionParser.BuildParsedProjects(request, filteredProjects.IncludedProjects);

        _logger.LogDebug(
            "Parsed {ParsedProjectCount} project(s) for target framework {TargetFramework} from {IncludedProjectCount} included project(s)",
            parsedProjects.Length, request.TargetFramework, filteredProjects.IncludedProjects.Length);

        return parsedProjects;
    }

    /// <inheritdoc />
    public async Task<string[]> DiscoverTargetFrameworksAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Getting filtered projects for {SolutionFilePath}, Include Regex: {IncludeRegex}, Exclude Regex: {ExcludeRegex}",
            solutionFilePath, regexToInclude, regexToExclude);

        var filteredProjects = await GetFilteredProjectsAsync(solutionFilePath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        var targetFrameworks = await _solutionParser
            .DiscoverTargetFrameworksAsync(filteredProjects.IncludedProjects, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug(
            "Discovered {TargetFrameworkCount} target framework(s) from {IncludedProjectCount} included project(s): {TargetFrameworks}",
            targetFrameworks.Length, filteredProjects.IncludedProjects.Length, string.Join(", ", targetFrameworks));

        return targetFrameworks;
    }

    private async Task<FilteredSolutionProjects> GetFilteredProjectsAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken)
    {
        var normalizedSolutionPath = Path.GetFullPath(solutionFilePath);

        // The solution file's last-write-time is part of the cache key so that a long-lived service
        // instance (e.g. the WPF host, which lives for the whole session) does not serve stale
        // project discovery when the .sln/.slnx file is modified between generation runs. Discovery
        // is cached on the instance, so without the timestamp the cache would return the previous
        // project set indefinitely after the solution changes on disk.
        var solutionFileLastWriteTimeUtc = File.GetLastWriteTimeUtc(normalizedSolutionPath);
        var discoveryKey = CreateDiscoveryKey(normalizedSolutionPath, solutionFileLastWriteTimeUtc, regexToInclude, regexToExclude);

        if (_cachedFilteredProjects is not null && _cachedDiscoveryKey == discoveryKey)
        {
            _logger.LogDebug("Using cached project discovery for {SolutionPath}", Path.GetFileName(normalizedSolutionPath));

            return _cachedFilteredProjects;
        }

        _logger.LogDebug("No cached project discovery for {SolutionPath}; discovering projects", Path.GetFileName(normalizedSolutionPath));

        var filteredProjects = await _solutionParser
            .DiscoverProjectsAsync(normalizedSolutionPath, regexToInclude, regexToExclude, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Discovered {IncludedCount} included, {ExcludedCount} excluded, {ImplicitlyExcludedCount} implicitly excluded of {TotalCount} project(s)",
            filteredProjects.IncludedProjects.Length,
            filteredProjects.ExcludedProjects.Length,
            filteredProjects.ImplicitlyExcludedProjects.Length,
            filteredProjects.AllProjects.Length);

        _cachedDiscoveryKey = discoveryKey;
        _cachedFilteredProjects = filteredProjects;

        _logger.LogDebug("Cached project discovery for {SolutionPath}", Path.GetFileName(normalizedSolutionPath));

        return filteredProjects;
    }

    private static GenericCacheKey<string, long, EquatableArray<string>, EquatableArray<string>> CreateDiscoveryKey(
        string solutionFilePath, DateTime solutionFileLastWriteTimeUtc, string[] regexToInclude, string[] regexToExclude)
    {
        // EquatableArray<string> is used instead of string[] because GenericCacheKey is a record:
        // records compare each component via EqualityComparer<T>.Default, and a plain array
        // compares by reference. EquatableArray is a struct that compares by length and element
        // content, so two keys built from the same regex patterns are considered equal regardless
        // of which array instances were used — the cache is keyed by content, not by instance.
        return new GenericCacheKey<string, long, EquatableArray<string>, EquatableArray<string>>(
            solutionFilePath,
            solutionFileLastWriteTimeUtc.Ticks,             // Caters for when the solution file changes on disk.
            new EquatableArray<string>(regexToInclude),
            new EquatableArray<string>(regexToExclude));
    }
}
