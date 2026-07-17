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
        /// <summary>Logs the pre-generation and diagram generation configuration.</summary>
        /// <param name="configFilename">The configuration file path.</param>
        /// <param name="logger">The logger instance.</param>
        public void LogConfiguration<T>(string configFilename, ILogger<T> logger)
        {
            logger.LogInformation("Configuration file: {ConfigFilePath}", Path.GetFullPath(configFilename));

            LogPreGeneratorConfiguration(document.PreGeneration, logger);
            LogGeneratorConfiguration(document.DiagramGenerator, logger);
        }
    }

    private static void LogPreGeneratorConfiguration<T>(PreGenerationConfig config, ILogger<T> logger)
    {
        logger.LogInformation("Pre-generation command configuration:");
        logger.LogInformation("  Enabled          : {Enabled}", config.Enabled);
        logger.LogInformation("  Command          : {Command}", config.Command);
        logger.LogInformation("  Arguments        : {Arguments}", config.Arguments);
        logger.LogInformation("  Working directory: {WorkingDirectory}", config.WorkingDirectory);
        logger.LogInformation("  Continue on fail : {ContinueOnFailure}", config.ContinueOnFailure);
    }

    private static void LogGeneratorConfiguration<T>(DependencyGeneratorConfig config, ILogger<T> logger)
    {
        logger.LogInformation("Diagram Generation configuration:");
        logger.LogInformation("  Resolved paths and options:");
        logger.LogInformation("    Solution path    : {SolutionPath}", config.Solution.SolutionPath);
        logger.LogInformation("    Export root      : {ExportRoot}", config.Export.RootPath);
        logger.LogInformation("    Clear contents   : {ClearContents}", config.Export.ClearContents);
        logger.LogInformation("    Diagram formats  : {Formats}", string.Join(", ", config.Diagram.Formats));
        logger.LogInformation("    Diagram direction: {Direction}", config.Diagram.Direction);
        logger.LogInformation("    Group name       : {GroupName}", config.Diagram.GroupName);
        logger.LogInformation("    Group alias      : {GroupAlias}", config.Diagram.GroupNameAlias);
        logger.LogInformation("    Grouping enabled : {GroupingEnabled}", config.Diagram.Grouping.Enabled);
        logger.LogInformation("    Image formats    : {ImageFormats}", string.Join(", ", config.Export.ImageFormats));

        logger.LogInformation("    Solution scopes:");

        logger.LogInformation("      Individual — Enabled: {IndividualEnabled}, IncludeDeps: {IndividualIncludeDeps}, TransitiveDepth: {IndividualTransitiveDepth}",
            config.Solution.Individual.Enabled,
            config.Solution.Individual.IncludeDependencies,
            config.Solution.Individual.TransitiveDepth);

        logger.LogInformation("      All        — Enabled: {AllEnabled}, IncludeDeps: {AllIncludeDeps}, TransitiveDepth: {AllTransitiveDepth}",
            config.Solution.All.Enabled,
            config.Solution.All.IncludeDependencies,
            config.Solution.All.TransitiveDepth);

        logger.LogInformation("    Regex include        : {RegexInclude}", string.Join(", ", config.Solution.RegexToInclude));
        logger.LogInformation("    Regex exclude        : {RegexExclude}", string.Join(", ", config.Solution.RegexToExclude));
        logger.LogInformation("    Packages to exclude  : {PackagesExclude}", string.Join(", ", config.Solution.PackagesToExclude));
        logger.LogInformation("    Frameworks to exclude: {FrameworksExclude}", string.Join(", ", config.Solution.FrameworksToExclude));
    }
}