using AllOverIt.Logging;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiagramGeneratorSample;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var logger = new ColorConsoleLogger();
        var configFile = GetConfigFilename(args);

        try
        {
            var options = GetGeneratorConfig(configFile);
            var generator = new DependencyGenerator(options, logger);

            await generator.CreateDiagramsAsync();

            logger
                .Write(ConsoleColor.Green, "The solution '")
                .Write(ConsoleColor.Yellow, Path.GetFileName(options.Projects.SolutionPath))
                .WriteLine(ConsoleColor.Green, "' has been processed.");
        }
        catch (Exception exception) when (exception is DependencyGeneratorException or ValidationException)
        {
            logger.WriteLine(ConsoleColor.Red, exception.Message);
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException or
                                                       ArgumentException or InvalidDataException)
        {
            WriteConfigurationError(logger, configFile, exception);
        }
        catch (Exception exception)
        {
            logger.WriteLine(ConsoleColor.Red, exception.Message);
        }
    }

    private static string GetConfigFilename(string[] args)
    {
        var configFile = "appsettings.json";

        if (args.Length == 2 && args[0].Equals("--configFile", StringComparison.InvariantCultureIgnoreCase))
        {
            configFile = args[1];
        }

        return configFile;
    }

    private static DependencyGeneratorConfig GetGeneratorConfig(string configFile)
    {
        configFile = Path.GetFullPath(configFile);

        var generatorConfig = new DependencyGeneratorConfig();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile(configFile, false, false)
            .Build();

        configuration.Bind("options", generatorConfig);

        return generatorConfig;
    }

    private static void WriteConfigurationError(ColorConsoleLogger logger, string configFile, Exception exception)
    {
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