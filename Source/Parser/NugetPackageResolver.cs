using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AllOverItDependencyDiagram.Parser
{
    internal sealed class NugetPackageResolver
    {
        private class NugetLogger : LoggerBase
        {
            private readonly IColorConsoleLogger _consoleLogger;

            public NugetLogger(IColorConsoleLogger consoleLogger)
            {
                _consoleLogger = consoleLogger.WhenNotNull(nameof(consoleLogger));
            }

            public override void Log(ILogMessage message)
            {
                // The message has whitespace at the start - leaving it in place
                _consoleLogger.WriteLine(ConsoleColor.Gray, message.Message);
            }

            public override Task LogAsync(ILogMessage message)
            {
                throw new NotImplementedException();
            }
        }

        private readonly IDictionary<(string, string), IEnumerable<PackageReference>> _nugetCache = new Dictionary<(string, string), IEnumerable<PackageReference>>();

        private readonly IReadOnlyCollection<SourceRepository> _sourceRepositories;
        private readonly int _maxDepth;
        private readonly IColorConsoleLogger _consoleLogger;
        private readonly ILogger _nugetLogger;

        public NugetPackageResolver(IEnumerable<NugetPackageFeed> packageFeeds, int maxDepth, IColorConsoleLogger consoleLogger)
        {
            _sourceRepositories = packageFeeds
                .WhenNotNullOrEmpty()
                .SelectAsReadOnlyCollection(GetSourceRepository);

            _maxDepth = maxDepth;
            _consoleLogger = consoleLogger.WhenNotNull();
            _nugetLogger = new NugetLogger(consoleLogger);
        }

        public Task<IReadOnlyCollection<PackageReference>> GetPackageReferences(string packageName, string packageVersion, string targetFramework)
        {
            _consoleLogger.Write(ConsoleColor.Green, "Processing package references for ");
            _consoleLogger.WriteLine(ConsoleColor.Yellow, $"{packageName} v{packageVersion} ({targetFramework})");

            return GetPackageReferencesRecursively(packageName, packageVersion, 1, targetFramework);
        }

        private async Task<IReadOnlyCollection<PackageReference>> GetPackageReferencesRecursively(string packageName, string packageVersion, int depth,
            string targetFramework)
        {
            if (depth > _maxDepth)
            {
                return Array.Empty<PackageReference>();
            }

            var cacheKey = (packageName, packageVersion);

            if (!_nugetCache.TryGetValue(cacheKey, out var packageReferences))
            {
                // Info on using the nuget client libraries
                // https://martinbjorkstrom.com/posts/2018-09-19-revisiting-nuget-client-libraries

                var package = new PackageIdentity(packageName, NuGetVersion.Parse(packageVersion));
                var nuGetFramework = NuGetFramework.ParseFolder(targetFramework);

                using (var cacheContext = new SourceCacheContext())
                {
                    foreach (var sourceRepository in _sourceRepositories)
                    {
                        var dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>();

                        var dependencyInfo = await dependencyInfoResource.ResolvePackage(package, nuGetFramework, cacheContext, _nugetLogger, CancellationToken.None);

                        if (dependencyInfo is null)
                        {
                            continue;
                        }

                        var dependencies = dependencyInfo.Dependencies;

                        var packageReferencesList = new List<PackageReference>();

                        foreach (var dependency in dependencies)
                        {
                            var dependencyName = dependency.Id;
                            var dependencyVersion = dependency.VersionRange.MinVersion.ToFullString();

                            var transitiveReferences = await GetPackageReferencesRecursively(dependencyName, dependencyVersion, depth + 1, targetFramework);

                            var packageReference = new PackageReference(true, depth)
                            {
                                Name = dependencyName,
                                Version = dependencyVersion,
                                TransitiveReferences = transitiveReferences.AsReadOnlyCollection()
                            };

                            packageReferencesList.Add(packageReference);
                        }

                        packageReferences = packageReferencesList;
                        
                        break;
                    }
                }

                if (packageReferences is null)
                {
                    throw new DependencyDiagramGeneratorException($"Could not resolve the package {packageName} v{packageVersion}.");
                }

                _nugetCache.Add(cacheKey, packageReferences);
            }

            return packageReferences?.AsReadOnlyCollection() ?? AllOverIt.Collections.Collection.EmptyReadOnly<PackageReference>();
        }

        private static SourceRepository GetSourceRepository(NugetPackageFeed feed)
        {
            var credentials = feed.Username.IsNotNullOrEmpty() && feed.Password.IsNotNullOrEmpty()
                ? new PackageSourceCredential(feed.SourceUri, feed.Username, feed.Password, true, null)
                : null;

            var packageSource = new PackageSource(feed.SourceUri)
            {
                Credentials = credentials
            };

            return Repository.Factory.GetCoreV3(packageSource);
        }
    }
}