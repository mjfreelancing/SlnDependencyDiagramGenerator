using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Enumerations;
using System.Reactive.Subjects;

namespace SlnDependencyStudio.Shared.PreGeneration;

/// <inheritdoc cref="IPreGenerationCommandRunner"/>
internal sealed class PreGenerationCommandRunner : IPreGenerationCommandRunner
{
    private readonly ILogger<PreGenerationCommandRunner> _logger;
    private readonly Subject<string> _stdoutSubject = new();
    private readonly Subject<string> _stderrSubject = new();

    /// <inheritdoc />
    public IObservable<string> StdOut => _stdoutSubject;

    /// <inheritdoc />
    public IObservable<string> StdErr => _stderrSubject;

    /// <summary>Initializes a new instance of <see cref="PreGenerationCommandRunner"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public PreGenerationCommandRunner(ILogger<PreGenerationCommandRunner> logger)
    {
        _logger = logger.WhenNotNull();
    }

    /// <inheritdoc />
    public async Task<PreGenerationCommandResult> RunAsync(PreGenerationConfig config, CancellationToken cancellationToken)
    {
        if (!config.Enabled)
        {
            return new PreGenerationCommandResult
            {
                Succeeded = true,
                ExitCode = 0
            };
        }

        _logger.LogInformation(
            "Executing pre-generation command: {Command} {Arguments}",
            config.Command,
            config.Arguments);

        var executorOptions = ProcessBuilder
            .For(config.Command)
            .WithNoWindow();

        if (config.WorkingDirectory.IsNotNullOrEmpty())
        {
            executorOptions = executorOptions.WithWorkingDirectory(config.WorkingDirectory);
        }

        if (config.Arguments.IsNotNullOrEmpty())
        {
            // Split the arguments string so each token is passed as a separate argument to the process.
            // Without splitting, the entire string is treated as a single quoted argument
            // (e.g. "restore file.sln" will be seen by the process as a single unknown command name).
            var args = config.Arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            executorOptions = executorOptions.WithArguments(args);
        }

        var executor = executorOptions
            .WithStandardOutputHandler((_, args) =>
            {
                if (args.Data is not null)
                {
                    _stdoutSubject.OnNext(args.Data);
                }
            })
            .WithErrorOutputHandler((_, args) =>
            {
                if (args.Data is not null)
                {
                    _stderrSubject.OnNext(args.Data);
                }
            })
            .BuildProcessExecutor();

        using (executor)
        {
            try
            {
                _logger.LogInformation("Pre-generation command started.");

                cancellationToken.ThrowIfCancellationRequested();

                var result = await executor.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Pre-generation command completed with exit code {ExitCode}.",
                    result.ExitCode);

                var succeeded = result.ExitCode == 0;

                return new PreGenerationCommandResult
                {
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
                    Succeeded = false,
                    ExitCode = StudioExitCode.PreGenerationCommandCancelled.Value,
                    ErrorMessage = "Pre-generation command was cancelled."
                };
            }
            catch (TimeoutException exception)
            {
                _logger.LogError(exception, "Pre-generation command timed out.");

                return new PreGenerationCommandResult
                {
                    Succeeded = false,
                    ExitCode = StudioExitCode.PreGenerationCommandTimeout.Value,
                    ErrorMessage = "Pre-generation command timed out."
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Pre-generation command failed with an unexpected error.");

                return new PreGenerationCommandResult
                {
                    Succeeded = false,
                    ExitCode = StudioExitCode.PreGenerationUnexpectedError.Value,
                    ErrorMessage = exception.Message
                };
            }
        }
    }
}
