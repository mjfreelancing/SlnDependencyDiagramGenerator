using AllOverIt.Logging;
using AllOverIt.Validation.Extensions;
using AllOverItDependencyDiagram.Generator;
using AllOverItDependencyDiagram.Validator;
using FluentValidation;
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
            var options = GetAppOptions();
            var logger = new ColorConsoleLogger();
            var generator = new ProjectDependencyGenerator(options, logger);

            try
            {
                await generator.CreateDiagramsAsync();

                logger
                    .WriteLine()
                    .Write(ConsoleColor.Green, "The solution '")
                    .Write(ConsoleColor.Yellow, Path.GetFileName(options.SolutionPath))
                    .WriteLine(ConsoleColor.Green, "' has been processed.");
            }
            catch (PackageReferenceNotResolvedException exception)
            {
                logger.WriteLine(ConsoleColor.Red, exception.Message);
            }
        }

        private static IDependencyGeneratorOptions GetAppOptions()
        {
            var options = new AppOptions();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", false, false)
                .AddUserSecrets<AppOptions>(true)
                .Build();

            configuration.Bind("Options", options);

            var validator = new AppOptionsValidator();
            validator.ValidateAndThrow(options);

            return options;
        }
    }
}