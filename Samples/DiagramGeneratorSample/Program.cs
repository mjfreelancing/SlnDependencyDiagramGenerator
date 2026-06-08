using AllOverIt.Extensions;
using AllOverIt.Logging;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using SlnDependencyDiagramGenerator.Generator.Discovery;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DiagramGeneratorSample;

internal class Program
{
    private sealed record ConfigurationSelection(string ConfigFile, string ConfigVariant);

    private static async Task Main(string[] args)
    {
        var logger = new ColorConsoleLogger();
        var configurationSelection = GetConfigurationSelection(args);

        try
        {
            var options = GetGeneratorConfig(configurationSelection);
            var generator = new DependencyGenerator();

            await generator.CreateDiagramsAsync(options, CancellationToken.None);

            logger
                .Write(ConsoleColor.Green, "The solution '")
                .Write(ConsoleColor.Yellow, Path.GetFileName(options.Projects.SolutionPath))
                .WriteLine(ConsoleColor.Green, "' has been processed.");
        }
        catch (Exception exception) when (exception is DependencyGeneratorException or ValidationException)
        {
            if (exception is DependencyGeneratorException)
            {
                // Intentionally use ToString() so runtime failures include type, stack trace,
                // and inner-exception details (Message alone hid root causes in prior debugging).
                logger.WriteLine(ConsoleColor.Red, exception.ToString());
            }
            else
            {
                logger.WriteLine(ConsoleColor.Red, exception.Message);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException or
                                                       ArgumentException or InvalidDataException)
        {
            // Show full exception details so the root cause is visible
            WriteConfigurationError(logger, configurationSelection.ConfigFile, exception);

            logger.WriteLine(ConsoleColor.DarkGray, exception.ToString());
        }
        catch (Exception exception)
        {
            // Keep full exception output here as well so assembly-load and resolver failures
            // are not reduced to a single message line.
            logger.WriteLine(ConsoleColor.Red, exception.ToString());
        }
    }

    private static ConfigurationSelection GetConfigurationSelection(string[] args)
    {
        string configFile = null;

        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index].Equals("--configFile", StringComparison.InvariantCultureIgnoreCase))
            {
                configFile = args[index + 1];
            }
        }

        configFile ??= "appsettings.json";

        // Read the optional variant from the environment (e.g. set via launchSettings.json).
        // When set, appsettings.<variant>.json is layered over appsettings.json.
        var configVariant = Environment.GetEnvironmentVariable("SETTINGS_VARIANT");

        return new ConfigurationSelection(configFile, configVariant);
    }

    private static DependencyGeneratorConfig GetGeneratorConfig(ConfigurationSelection configurationSelection)
    {
        var configFile = Path.GetFullPath(configurationSelection.ConfigFile);
        var configDirectory = Path.GetDirectoryName(configFile) ?? AppDomain.CurrentDomain.BaseDirectory;
        var configFileName = Path.GetFileName(configFile);

        var generatorConfig = new DependencyGeneratorConfig();

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(configDirectory)
            .AddJsonFile(configFileName, false, false);

        // When configured, layer appsettings.<variant>.json over appsettings.json,
        // similar to environment-specific config behavior.
        if (!configurationSelection.ConfigVariant.IsNullOrEmpty())
        {
            var variantFile = $"appsettings.{configurationSelection.ConfigVariant}.json";
            configurationBuilder.AddJsonFile(variantFile, true, false);
        }

        var configuration = configurationBuilder.Build();

        configuration.Bind("options", generatorConfig);

        return generatorConfig;
    }

    private static void WriteConfigurationError(ColorConsoleLogger logger, string configFile, Exception exception)
    {
        configFile ??= "appsettings.json";

        logger
            .WriteLine()
            .WriteLine(ConsoleColor.Red, "Failed to load configuration.")
            .Write(ConsoleColor.White, "File: ")
            .WriteLine(ConsoleColor.Yellow, Path.GetFullPath(configFile))
            .WriteLine()
            .WriteLine(ConsoleColor.White, "The configuration file could not be parsed or bound to the expected options type.")
            .WriteLine(ConsoleColor.DarkGray, exception.Message);
    }
}