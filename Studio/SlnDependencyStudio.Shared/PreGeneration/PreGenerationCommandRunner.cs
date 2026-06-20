using AllOverIt.Extensions;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using Microsoft.Extensions.Logging;

namespace SlnDependencyStudio.Shared.PreGeneration;

/// <inheritdoc cref="IPreGenerationCommandRunner"/>
internal sealed class PreGenerationCommandRunner : IPreGenerationCommandRunner
{
    private readonly ILogger<PreGenerationCommandRunner> _logger;

    /// <summary>Initializes a new instance of <see cref="PreGenerationCommandRunner"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public PreGenerationCommandRunner(ILogger<PreGenerationCommandRunner> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PreGenerationCommandResult> RunAsync(PreGenerationConfig config, CancellationToken cancellationToken)
    {
        if (!config.Enabled || config.Command.IsNullOrEmpty())
        {
            return new PreGenerationCommandResult
            {
                CommandAttempted = false,
                Succeeded = true
            };
        }

        _logger.LogInformation(
            "Executing pre-generation command: {Command} {Arguments}",
            config.Command,
            config.Arguments);

        var workingDirectory = config.WorkingDirectory.IsNotNullOrEmpty()
            ? config.WorkingDirectory
            : null;

        var executorOptions = ProcessBuilder.For(config.Command);

        if (config.WorkingDirectory.IsNotNullOrEmpty())
        {
            executorOptions = executorOptions.WithWorkingDirectory(config.WorkingDirectory);
        }

        if (config.Arguments.IsNotNullOrEmpty())
        {
            executorOptions = executorOptions.WithArguments(config.Arguments);
        }

        var executor = executorOptions
            .WithStandardOutputHandler((_, args) =>
            {
                if (args.Data is not null)
                {
                    _logger.LogInformation("{Output}", args.Data);
                }
            })
            .WithErrorOutputHandler((_, args) =>
            {
                if (args.Data is not null)
                {
                    _logger.LogWarning("{Output}", args.Data);
                }
            })
            .BuildProcessExecutor();

        using (executor)
        {
            try
            {
                _logger.LogInformation("Pre-generation command started.");

                var result = await executor.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Pre-generation command completed with exit code {ExitCode}.",
                    result.ExitCode);

                var succeeded = result.ExitCode == 0;

                return new PreGenerationCommandResult
                {
                    CommandAttempted = true,
                    Succeeded = succeeded,
                    ExitCode = result.ExitCode,
                    ErrorMessage = succeeded
                        ? null
                        : $"Pre-generation command exited with code {result.ExitCode}."
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Pre-generation command was cancelled.");

                return new PreGenerationCommandResult
                {
                    CommandAttempted = true,
                    Succeeded = false,
                    ErrorMessage = "Pre-generation command was cancelled."
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Pre-generation command failed with an unexpected error.");

                return new PreGenerationCommandResult
                {
                    CommandAttempted = true,
                    Succeeded = false,
                    ErrorMessage = exception.Message
                };
            }
        }
    }
}
