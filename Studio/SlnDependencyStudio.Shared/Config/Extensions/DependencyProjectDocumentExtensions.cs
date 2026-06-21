using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyStudio.Shared.Config.Extensions;

public static class DependencyProjectDocumentExtensions
{
    extension(DependencyProjectDocument document)
    {
        public void LogConfiguration<T>(string configFilename, ILogger<T> _logger)
        {
            _logger.LogInformation("Configuration file: {ConfigFilePath}", Path.GetFullPath(configFilename));

            LogPreGeneratorConfiguration(document.PreGeneration, _logger);
            LogGeneratorConfiguration(document.DiagramGenerator, _logger);
        }
    }

    private static void LogPreGeneratorConfiguration<T>(PreGenerationConfig config, ILogger<T> _logger)
    {
        _logger.LogInformation("Pre-generation command configuration:");
        _logger.LogInformation("  Enabled          : {Enabled}", config.Enabled);
        _logger.LogInformation("  Command          : {Command}", config.Command);
        _logger.LogInformation("  Arguments        : {Arguments}", config.Arguments);
        _logger.LogInformation("  Working directory: {WorkingDirectory}", config.WorkingDirectory);
        _logger.LogInformation("  Continue on fail : {ContinueOnFailure}", config.ContinueOnFailure);
    }

    private static void LogGeneratorConfiguration<T>(DependencyGeneratorConfig config, ILogger<T> _logger)
    {
        _logger.LogInformation("Diagram Generation configuration:");
        _logger.LogInformation("  Resolved paths and options:");
        _logger.LogInformation("    Solution path    : {SolutionPath}", config.Projects.SolutionPath);
        _logger.LogInformation("    Export root      : {ExportRoot}", config.Export.RootPath);
        _logger.LogInformation("    Clear contents   : {ClearContents}", config.Export.ClearContents);
        _logger.LogInformation("    Diagram formats  : {Formats}", string.Join(", ", config.Diagram.Formats));
        _logger.LogInformation("    Diagram direction: {Direction}", config.Diagram.Direction);
        _logger.LogInformation("    Group name       : {GroupName}", config.Diagram.GroupName);
        _logger.LogInformation("    Group alias      : {GroupAlias}", config.Diagram.GroupNameAlias);
        _logger.LogInformation("    Grouping enabled : {GroupingEnabled}", config.Diagram.Grouping.Enabled);
        _logger.LogInformation("    Image formats    : {ImageFormats}", string.Join(", ", config.Export.ImageFormats));

        _logger.LogInformation("    Project scopes:");

        _logger.LogInformation("      Individual — Enabled: {IndividualEnabled}, IncludeDeps: {IndividualIncludeDeps}, TransitiveDepth: {IndividualTransitiveDepth}",
            config.Projects.Individual.Enabled,
            config.Projects.Individual.IncludeDependencies,
            config.Projects.Individual.TransitiveDepth);

        _logger.LogInformation("      All        — Enabled: {AllEnabled}, IncludeDeps: {AllIncludeDeps}, TransitiveDepth: {AllTransitiveDepth}",
            config.Projects.All.Enabled,
            config.Projects.All.IncludeDependencies,
            config.Projects.All.TransitiveDepth);

        _logger.LogInformation("    Regex include        : {RegexInclude}", string.Join(", ", config.Projects.RegexToInclude));
        _logger.LogInformation("    Regex exclude        : {RegexExclude}", string.Join(", ", config.Projects.RegexToExclude));
        _logger.LogInformation("    Packages to exclude  : {PackagesExclude}", string.Join(", ", config.Projects.PackagesToExclude));
        _logger.LogInformation("    Frameworks to exclude: {FrameworksExclude}", string.Join(", ", config.Projects.FrameworksToExclude));
    }
}