using AllOverIt.Logging;
using AllOverItDependencyDiagram.Generator;
using Microsoft.Extensions.Configuration;
using SlnDependencyDiagramGenerator.Exceptions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AllOverItDependencyDiagram
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var configFile = GetConfigFilename(args);
            var options = GetGeneratorConfig(configFile);
            var logger = new ColorConsoleLogger();
            var generator = new DependencyGenerator(options, logger);

            try
            {
                await generator.CreateDiagramsAsync();

                logger
                    .Write(ConsoleColor.Green, "The solution '")
                    .Write(ConsoleColor.Yellow, Path.GetFileName(options.Projects.SolutionPath))
                    .WriteLine(ConsoleColor.Green, "' has been processed.");
            }
            catch (DependencyGeneratorException exception)
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

        private static AppOptions GetGeneratorConfig(string configFile)
        {
            var generatorConfig = new AppOptions();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(configFile, false, false)

#if DEBUG
                .AddUserSecrets<AppOptions>(true)
#endif

                .Build();

            configuration.Bind("options", generatorConfig);

            return generatorConfig;
        }
    }
}