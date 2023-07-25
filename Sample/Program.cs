using AllOverIt.Logging;
using AllOverIt.Validation.Extensions;
using AllOverItDependencyDiagram.Generator;
using AllOverItDependencyDiagram.Validator;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AllOverItDependencyDiagram
{
    internal class Program
    {
        static async Task Main()
        {
            var options = GetAppOptions();
            var logger = new ColorConsoleLogger();
            var generator = new ProjectDependencyGenerator(options, logger);

            await generator.CreateDiagramsAsync();

            Console.WriteLine();
            Console.WriteLine($"The solution '{Path.GetFileName(options.SolutionPath)}' has been processed.");
        }

        private static IProjectDependencyGeneratorOptions GetAppOptions()
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