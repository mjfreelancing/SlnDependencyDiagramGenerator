using Microsoft.Extensions.Configuration;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyStudio.Cli;

/// <summary>Loads and validates <see cref="DependencyGeneratorConfig"/> from JSON files.</summary>
internal sealed class ConfigLoader
{
    /// <summary>Loads a generator config from a JSON file.</summary>
    /// <param name="configFilePath">The path to the configuration JSON file.</param>
    /// <returns>The deserialized and validated configuration.</returns>
    public DependencyGeneratorConfig Load(string configFilePath)
    {
        var fullPath = Path.GetFullPath(configFilePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException($"Cannot determine directory from path: {configFilePath}");
        var fileName = Path.GetFileName(fullPath);

        var generatorConfig = new DependencyGeneratorConfig();

        var configRoot = new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile(fileName, optional: false, reloadOnChange: false)
            .Build();

        configRoot.Bind("options", generatorConfig);

        ResolveRelativePaths(generatorConfig, directory);

        return generatorConfig;
    }

    /// <summary>
    /// Resolves relative <c>SolutionPath</c> and <c>RootPath</c> values against the config file's directory.
    /// Without this, <see cref="Path.GetFullPath"/> inside the validator resolves them relative to the
    /// current working directory, which varies depending on where the CLI is launched from. Config-relative
    /// paths are the intended contract — the same config should work regardless of the launch location.
    /// </summary>
    private static void ResolveRelativePaths(DependencyGeneratorConfig config, string configDirectory)
    {
        if (!Path.IsPathRooted(config.Projects.SolutionPath))
        {
            config.Projects.SolutionPath = Path.GetFullPath(
                Path.Combine(configDirectory, config.Projects.SolutionPath));
        }

        if (!Path.IsPathRooted(config.Export.RootPath))
        {
            config.Export.RootPath = Path.GetFullPath(
                Path.Combine(configDirectory, config.Export.RootPath));
        }
    }
}