using AllOverIt.Extensions;

namespace SlnDependencyDiagramGenerator.Tests.Integration.Support;

/// <summary>Locates fixture paths for integration tests.</summary>
internal static class FixtureLocator
{
    private const string IntegrationProjectFileName = "SlnDependencyDiagramGenerator.Tests.Integration.csproj";

    /// <summary>Gets the absolute path to the integration test project directory.</summary>
    /// <returns>The integration test project directory.</returns>
    public static string GetIntegrationProjectDirectory()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var projectFilePath = Path.Combine(currentDirectory.FullName, IntegrationProjectFileName);

            if (File.Exists(projectFilePath))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not locate {IntegrationProjectFileName} from {AppContext.BaseDirectory}.");
    }

    /// <summary>Gets the absolute path to the fixture root directory.</summary>
    /// <returns>The fixture root directory.</returns>
    public static string GetFixturesDirectory()
    {
        return Path.Combine(GetIntegrationProjectDirectory(), "Fixtures");
    }

    /// <summary>Gets the absolute path to a fixture directory by name.</summary>
    /// <param name="fixtureName">The fixture folder name.</param>
    /// <returns>The fixture directory path.</returns>
    public static string GetFixtureDirectory(string fixtureName)
    {
        if (fixtureName.IsNullOrEmpty())
        {
            throw new ArgumentException("A fixture name is required.", nameof(fixtureName));
        }

        return Path.Combine(GetFixturesDirectory(), fixtureName);
    }

    /// <summary>Gets the fixture solution path where the solution file name matches the fixture name.</summary>
    /// <param name="fixtureName">The fixture folder name and default solution file stem.</param>
    /// <returns>The absolute fixture solution path.</returns>
    public static string GetFixtureSolutionPath(string fixtureName)
    {
        var fixtureDirectory = GetFixtureDirectory(fixtureName);

        return Path.Combine(fixtureDirectory, $"{fixtureName}.sln");
    }
}
