using AllOverIt.Assertion;
using AllOverIt.GenericHost;
using FluentValidation;
using Microsoft.Extensions.Logging;
using SlnDependencyDiagramGenerator.Generator;
using System.CommandLine;

namespace SlnDependencyStudio.Cli;

/// <summary>CLI entry point that parses commands and delegates to services.</summary>
internal sealed class App : ConsoleAppBase
{
    private readonly DependencyGenerator _generator;
    private readonly ConfigLoader _configLoader;
    private readonly ILogger<App> _logger;

    /// <summary>Initializes a new instance of <see cref="App"/>.</summary>
    /// <param name="generator">The dependency diagram generator.</param>
    /// <param name="configLoader">The configuration file loader.</param>
    /// <param name="logger">The logger instance.</param>
    public App(DependencyGenerator generator, ConfigLoader configLoader, ILogger<App> logger)
    {
        _generator = generator.WhenNotNull();
        _configLoader = configLoader.WhenNotNull();
        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var configFileOption = new Option<string>("--configFile", "--cf")
        {
            Description = "Path to the configuration JSON file",
            Required = true
        };

        var runCommand = new Command("run", "Generate dependency diagrams from a configuration file")
        {
            configFileOption
        };

        runCommand.SetAction(async parseResult =>
        {
            var configFile = parseResult.GetValue(configFileOption)!;
            await HandleRunAsync(configFile);
        });

        var validateCommand = new Command("validate", "Validate a configuration file without running generation")
        {
            configFileOption
        };

        validateCommand.SetAction(parseResult =>
        {
            var configFile = parseResult.GetValue(configFileOption)!;
            HandleValidate(configFile);
        });

        var root = new RootCommand("SlnDependencyStudio CLI — dependency diagram generation")
        {
            runCommand,
            validateCommand
        };

        var parseResult = root.Parse(Environment.GetCommandLineArgs()[1..]);

        var exitCode = await parseResult.InvokeAsync(cancellationToken: cancellationToken);
        ExitCode = exitCode;
    }

    private async Task HandleRunAsync(string configFile)
    {
        try
        {
            var config = _configLoader.Load(configFile);

            _logger.LogInformation("Generating diagrams for solution: {SolutionPath}", config.Projects.SolutionPath);
            _logger.LogInformation("Export root: {ExportRoot}", config.Export.RootPath);

            await _generator.CreateDiagramsAsync(config, CancellationToken.None);

            _logger.LogInformation("Generation complete.");
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
        }
        catch (FileNotFoundException exception)
        {
            _logger.LogError("File not found: {Message}", exception.Message);
        }
    }

    private void HandleValidate(string configFile)
    {
        try
        {
            var config = _configLoader.Load(configFile);
            DependencyGenerator.ValidateConfiguration(config);

            _logger.LogInformation("Configuration is valid.");
        }
        catch (ValidationException exception)
        {
            WriteValidationErrors(exception);
        }
        catch (FileNotFoundException exception)
        {
            _logger.LogError("File not found: {Message}", exception.Message);
        }
    }

    private void WriteValidationErrors(ValidationException exception)
    {
        _logger.LogError("Configuration validation failed:");

        foreach (var error in exception.Errors)
        {
            _logger.LogError("  - {ErrorMessage}", error.ErrorMessage);
        }
    }
}