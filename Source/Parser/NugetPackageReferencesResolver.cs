using AllOverIt.Assertion;
using AllOverIt.Extensions;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AllOverItDependencyDiagram.Parser
{
    internal sealed class NugetPackageReferencesResolver
    {
        public static readonly string[] TargetFrameworkOrderPreference = new[]
        {
            // Keep all of these even as supported frameworks are removed
            "net8.0",
            "net7.0",
            "net6.0",
            "net5.0",
            ".netcoreapp3.1",
            ".netstandard2.1",
            ".netstandard2.0"
        };

        private readonly IDictionary<(string, string), IEnumerable<PackageReference>> _nugetCache = new Dictionary<(string, string), IEnumerable<PackageReference>>();
        private readonly int _maxDepth;

        public NugetPackageReferencesResolver(int maxDepth = 1)
        {
            _maxDepth = maxDepth;
        }

        public Task<IReadOnlyCollection<PackageReference>> GetPackageReferences(string packageName, string packageVersion, string targetFramework)
        {
            return GetPackageReferencesRecursively(packageName, packageVersion, 1, targetFramework);
        }

        private async Task<IReadOnlyCollection<PackageReference>> GetPackageReferencesRecursively(string packageName, string packageVersion, int depth, string targetFramework)
        {
            if (depth > _maxDepth)
            {
                return Array.Empty<PackageReference>();
            }

            packageVersion = GetAssumedVersion(packageVersion);

            var cacheKey = (packageName, packageVersion);

            if (!_nugetCache.TryGetValue(cacheKey, out var packageReferences))
            {
                var apiUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName}/{packageVersion}/{packageName}.nuspec";
                var nuspecXml = await apiUrl.GetStringAsync();
                var nuspec = XDocument.Parse(nuspecXml);

                var ns = nuspec.Root.Name.Namespace;

                var dependenciesByFramework = nuspec.Descendants(ns + "group")
                    .Where(grp => grp.Attribute("targetFramework") != null) // ? the targetFramework attribute is always present
                    .GroupBy(grp => grp.Attribute("targetFramework").Value)
                    .ToDictionary(
                        grp => grp.Key,
                        grp => grp.Descendants(ns + "dependency")
                                  .SelectAsReadOnlyCollection(element => new
                                  {
                                      Id = element.Attribute("id").Value,
                                      Version = element.Attribute("version").Value
                                  })
                    );

                if (dependenciesByFramework.Any())
                {
                    var useTarget = UseTargetFramework(dependenciesByFramework.Keys);

                    var dependencies = dependenciesByFramework
                        .Single(kvp => useTarget.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        .Value;

                    var packageReferencesList = new List<PackageReference>();

                    foreach (var dependency in dependencies)
                    {
                        var dependencyName = dependency.Id;
                        var dependencyVersion = GetAssumedVersion(dependency.Version);

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
                }

                _nugetCache.Add(cacheKey, packageReferences);
            }

            return packageReferences?.AsReadOnlyCollection() ?? AllOverIt.Collections.Collection.EmptyReadOnly<PackageReference>();
        }

        private static string GetAssumedVersion(string packageVersion)
        {
            if (packageVersion[0] == '[')
            {
                // Need to handle versions such as [2.1.1, 3.0.0)
                packageVersion = packageVersion[1..^1].Split(",").First().Trim();
            }

            return packageVersion;
        }

        private static string UseTargetFramework(IEnumerable<string> targetFrameworks)
        {
            // Processing like this to ensure the list is provided in the order of TargetFrameworks.Descending
            var availableTargets = targetFrameworks.Select(key => key.ToLowerInvariant());
            availableTargets = TargetFrameworkOrderPreference.Intersect(availableTargets);

            var useTarget = availableTargets.FirstOrDefault();

            Throw<InvalidOperationException>.WhenNull(useTarget, $"Cannot find usable targetFramework from {string.Join(", ", targetFrameworks)}");

            return useTarget;
        }
    }
}