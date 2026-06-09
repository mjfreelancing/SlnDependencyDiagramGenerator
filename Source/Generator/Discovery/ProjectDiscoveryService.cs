using AllOverIt.Extensions;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyDiagramGenerator.Parser.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyDiagramGenerator.Generator.Discovery;

/// <summary>Provides project discovery and parsing services by wrapping SolutionParser
/// and solution project resolvers.</summary>
internal sealed class ProjectDiscoveryService : IProjectDiscoveryService
{
    private readonly SolutionParser _solutionParser = new();

    /// <inheritdoc />
    public async Task<ProjectDiscoveryResult> DiscoverProjectsAsync(string solutionFilePath, string[] regexToInclude,
        string[] regexToExclude, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var extension = GetSolutionExtension(solutionFilePath);
        var resolvers = CreateResolvers();

        if (!resolvers.TryGetValue(extension, out var resolver))
        {
            var supported = string.Join(", ", resolvers.Keys.OrderBy(item => item));

            throw new DependencyGeneratorException(
                $"Unsupported solution extension '{extension}'. Supported extensions are {supported}. Path: {solutionFilePath}");
        }

        var allProjects = await resolver.GetProjectsAsync(solutionFilePath, cancellationToken).ConfigureAwait(false);

        var includeRegexes = regexToInclude.SelectToArray(regex => new Regex(regex));
        var excludeRegexes = regexToExclude.SelectToArray(regex => new Regex(regex));

        var included = new List<string>();
        var excluded = new List<string>();
        var implicitlyExcluded = new List<string>();

        foreach (var project in allProjects)
        {
            var isIncluded = includeRegexes.Any(regex => regex.Matches(project.AbsolutePath).Count > 0);

            if (!isIncluded)
            {
                implicitlyExcluded.Add(project.AbsolutePath);
                continue;
            }

            var isExcluded = excludeRegexes.Length > 0 &&
                excludeRegexes.Any(regex => regex.Matches(project.AbsolutePath).Count > 0);

            if (isExcluded)
            {
                excluded.Add(project.AbsolutePath);
            }
            else
            {
                included.Add(project.AbsolutePath);
            }
        }

        return new ProjectDiscoveryResult
        {
            AllProjectPaths = allProjects.Select(project => project.AbsolutePath).ToArray(),
            IncludedProjectPaths = [.. included],
            ExcludedProjectPaths = [.. excluded],
            ImplicitlyExcludedProjectPaths = [.. implicitlyExcluded]
        };
    }

    /// <inheritdoc />
    public Task<SolutionProject[]> ParseProjectsAsync(SolutionParseRequest request, CancellationToken cancellationToken)
    {
        return _solutionParser.ParseAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<string[]> DiscoverTargetFrameworksAsync(string solutionFilePath, string[] regexToInclude, string[] regexToExclude,
        CancellationToken cancellationToken)
    {
        return _solutionParser.DiscoverTargetFrameworksAsync(solutionFilePath, regexToInclude, regexToExclude, cancellationToken);
    }

    private static string GetSolutionExtension(string solutionFilePath)
    {
        return Path.GetExtension(solutionFilePath);
    }

    private static Dictionary<string, ISolutionProjectResolver> CreateResolvers()
    {
        return new Dictionary<string, ISolutionProjectResolver>(StringComparer.OrdinalIgnoreCase)
        {
            [".sln"] = new SlnSolutionProjectResolver(),
            [".slnx"] = new SlnxSolutionProjectResolver()
        };
    }
}
