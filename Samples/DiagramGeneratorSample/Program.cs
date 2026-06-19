using AllOverIt.Extensions;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Generator;
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
        using var serviceProvider = CreateServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var configurationSelection = GetConfigurationSelection(args);

        try
        {
            var options = GetGeneratorConfig(configurationSelection);
            var generator = serviceProvider.GetRequiredService<DependencyGenerator>();

            await generator.CreateDiagramsAsync(options, CancellationToken.None);

            logger.LogInformation("The solution '{SolutionName}' has been processed.", Path.GetFileName(options.Projects.SolutionPath));
        }
        catch (Exception exception) when (exception is DependencyGeneratorException or ValidationException)
        {
            if (exception is DependencyGeneratorException)
            {
                // Intentionally use ToString() so runtime failures include type, stack trace,
                // and inner-exception details (Message alone hid root causes in prior debugging).
                logger.LogError("{ExceptionText}", exception.ToString());
            }
            else
            {
                logger.LogError("{ErrorMessage}", exception.Message);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException or
                                                       ArgumentException or InvalidDataException)
        {
            // Show full exception details so the root cause is visible
            WriteConfigurationError(logger, configurationSelection.ConfigFile, exception);

            logger.LogError("{ExceptionText}", exception.ToString());
        }
        catch (Exception exception)
        {
            // Keep full exception output here as well so assembly-load and resolver failures
            // are not reduced to a single message line.
            logger.LogError("{ExceptionText}", exception.ToString());
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        services
            .AddLogging(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    });
            })
            .AddSlnDependencyGenerator();

        return services.BuildServiceProvider();
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
        ResolveRelativePaths(generatorConfig, configDirectory);

        return generatorConfig;
    }

    private static void ResolveRelativePaths(DependencyGeneratorConfig config, string configDirectory)
    {
        if (!string.IsNullOrWhiteSpace(config.Projects.SolutionPath) &&
            !Path.IsPathRooted(config.Projects.SolutionPath))
        {
            config.Projects.SolutionPath = Path.GetFullPath(
                Path.Combine(configDirectory, config.Projects.SolutionPath));
        }

        if (!string.IsNullOrWhiteSpace(config.Export.RootPath) &&
            !Path.IsPathRooted(config.Export.RootPath))
        {
            config.Export.RootPath = Path.GetFullPath(
                Path.Combine(configDirectory, config.Export.RootPath));
        }
    }

    private static void WriteConfigurationError(ILogger logger, string configFile, Exception exception)
    {
        configFile ??= "appsettings.json";

        logger.LogError("Failed to load configuration.");
        logger.LogError("File: {ConfigFilePath}", Path.GetFullPath(configFile));
        logger.LogError("The configuration file could not be parsed or bound to the expected options type.");
        logger.LogError("{ErrorMessage}", exception.Message);
    }
}