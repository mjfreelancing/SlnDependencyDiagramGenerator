using AllOverIt.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Extensions;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Cli.Handlers.Validate;
using SlnDependencyStudio.Cli.Setup;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Shared.Extensions;

namespace SlnDependencyStudio.Cli.Tests.Integration.Support;

/// <summary>Helper methods for CLI integration testing.</summary>
internal static class CliTestHarness
{
    /// <summary>Parses and invokes the given command line, returning the exit code.
    /// Simulates the same flow as <see cref="Cli.App.StartAsync"/> with controlled args.</summary>
    public static async Task<int> InvokeAsync(string commandLine)
    {
        var args = ParseCommandLine(commandLine).ToArray();
        var exitCode = 0;

        var services = new ServiceCollection();
        services.AddLogging();
        var (_, validationRegistry) = services.AddSlnDependencyDiagramGenerator();
        services.AddSlnDependencyStudio(validationRegistry);

        // Auto-register CLI-specific services (mirrors Program.cs)
        services.AutoRegisterScoped<Cli.DependencyRegistrar, IStudioScopedDependency>(config =>
        {
            config.Filter((serviceType, _) => serviceType != typeof(IStudioScopedDependency));
        });

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var validateHandler = scope.ServiceProvider.GetRequiredService<ICommandLineValidateHandler>();
        var runHandler = scope.ServiceProvider.GetRequiredService<ICommandLineRunHandler>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("CLIIntegrationTest");

        var root = new CommandLineSetup()
            .AddValidate(validateHandler, code => exitCode = code)
            .AddRun(runHandler, code => exitCode = code)
            .Build(logger, out _);

        var parseResult = root.Parse(args);

        if (parseResult.Errors.Count > 0)
        {
            return 1001; // (int)StudioCliExitCode.CommandLineParseFailed
        }

        await parseResult.InvokeAsync(cancellationToken: CancellationToken.None);

        return exitCode;
    }

    /// <summary>Splits a command-line string into arguments, respecting quoted strings.</summary>
    private static List<string> ParseCommandLine(string commandLine)
    {
        var args = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;

        foreach (var c in commandLine)
        {
            if (c == '"')
            {
                inQuote = !inQuote;
            }
            else if (c == ' ' && !inQuote)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }

        return args;
    }
}