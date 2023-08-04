using AllOverIt.Logging;
using AllOverItDependencyDiagram.Generator;
using Microsoft.Extensions.Configuration;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Exceptions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AllOverItDependencyDiagram
{
    internal class Program
    {
        private static async Task Main()
        {
            var options = GetGeneratorConfig();
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

        private static DependencyGeneratorConfig GetGeneratorConfig()
        {
            var generatorConfig = new AppOptions();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false, false)
                .AddUserSecrets<AppOptions>(true)
                .Build();

            configuration.Bind("options", generatorConfig);

            return generatorConfig;
        }
    }
}