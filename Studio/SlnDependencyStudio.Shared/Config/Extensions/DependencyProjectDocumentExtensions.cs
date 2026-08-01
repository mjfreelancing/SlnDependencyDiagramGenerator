using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyStudio.Shared.Config.Extensions;

public static class DependencyProjectDocumentExtensions
{
    /// <summary>
    /// Defines extension methods for <see cref="DependencyProjectDocument"/>.
    /// </summary>
    /// <param name="document">The dependency project document.</param>
    extension(DependencyProjectDocument document)
    {
        /// <summary>Logs the pre-generation, diagram generation and post-generation configuration.</summary>
        /// <param name="configFilename">The configuration file path.</param>
        /// <param name="logger">The logger instance.</param>
        public void LogConfiguration<T>(string configFilename, ILogger<T> logger)
        {
            logger.LogDebug("Configuration file: {ConfigFilePath}", Path.GetFullPath(configFilename));

            LogPreGeneratorConfiguration(document.PreGeneration, logger);
            LogGeneratorConfiguration(document.DiagramGenerator, logger);
            LogPostGeneratorConfiguration(document.PostGeneration, logger);
        }
    }

    private static void LogPreGeneratorConfiguration<T>(PreGenerationConfig config, ILogger<T> logger)
    {
        logger.LogDebug("Pre-generation command configuration:");
        logger.LogDebug("  Enabled          : {Enabled}", config.Enabled);
        logger.LogDebug("  Command          : {Command}", config.Command);
        logger.LogDebug("  Arguments        : {Arguments}", config.Arguments);
        logger.LogDebug("  Working directory: {WorkingDirectory}", config.WorkingDirectory);
        logger.LogDebug("  Continue on fail : {ContinueOnFailure}", config.ContinueOnFailure);
    }

    private static void LogPostGeneratorConfiguration<T>(PostGenerationConfig config, ILogger<T> logger)
    {
        logger.LogDebug("Post-generation command configuration:");
        logger.LogDebug("  Enabled          : {Enabled}", config.Enabled);
        logger.LogDebug("  Command          : {Command}", config.Command);
        logger.LogDebug("  Arguments        : {Arguments}", config.Arguments);
        logger.LogDebug("  Working directory: {WorkingDirectory}", config.WorkingDirectory);
    }

    private static void LogGeneratorConfiguration<T>(DependencyGeneratorConfig config, ILogger<T> logger)
    {
        logger.LogDebug("Diagram Generation configuration:");
        logger.LogDebug("  Resolved paths and options:");
        logger.LogDebug("    Solution path    : {SolutionPath}", config.Solution.SolutionPath);
        logger.LogDebug("    Export root      : {ExportRoot}", config.Export.RootPath);
        logger.LogDebug("    Clear contents   : {ClearContents}", config.Export.ClearContents);
        logger.LogDebug("    Diagram formats  : {Formats}", string.Join(", ", config.Diagram.Formats));
        logger.LogDebug("    Diagram direction: {Direction}", config.Diagram.Direction);
        logger.LogDebug("    Group name       : {GroupName}", config.Diagram.GroupName);
        logger.LogDebug("    Group alias      : {GroupAlias}", config.Diagram.GroupNameAlias);
        logger.LogDebug("    Grouping enabled : {GroupingEnabled}", config.Diagram.Grouping.Enabled);
        logger.LogDebug("    Image formats    : {ImageFormats}", string.Join(", ", config.Export.ImageFormats));

        logger.LogDebug("    Solution scopes:");

        logger.LogDebug("      Individual — Enabled: {IndividualEnabled}, IncludeDeps: {IndividualIncludeDeps}, TransitiveDepth: {IndividualTransitiveDepth}",
            config.Solution.Individual.Enabled,
            config.Solution.Individual.IncludeDependencies,
            config.Solution.Individual.TransitiveDepth);

        logger.LogDebug("      All        — Enabled: {AllEnabled}, IncludeDeps: {AllIncludeDeps}, TransitiveDepth: {AllTransitiveDepth}",
            config.Solution.All.Enabled,
            config.Solution.All.IncludeDependencies,
            config.Solution.All.TransitiveDepth);

        logger.LogDebug("    Regex include        : {RegexInclude}", string.Join(", ", config.Solution.RegexToInclude));
        logger.LogDebug("    Regex exclude        : {RegexExclude}", string.Join(", ", config.Solution.RegexToExclude));
        logger.LogDebug("    Packages to exclude  : {PackagesExclude}", string.Join(", ", config.Solution.PackagesToExclude));
        logger.LogDebug("    Frameworks to exclude: {FrameworksExclude}", string.Join(", ", config.Solution.FrameworksToExclude));
    }
}