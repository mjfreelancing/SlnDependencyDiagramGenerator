using AllOverIt.Assertion;
using AllOverIt.Extensions;
using AllOverIt.Process;
using AllOverIt.Process.Extensions;
using Microsoft.Extensions.Logging;
using System.Reactive.Subjects;

namespace SlnDependencyStudio.Shared.ProcessExecution;

/// <summary>
/// Base class for pipeline command runners that execute an external process via <see cref="ProcessBuilder"/>,
/// stream standard output/error as they arrive, and map the outcome to a typed <typeparamref name="TResult"/>.
/// </summary>
/// <typeparam name="TResult">The result type describing the command outcome.</typeparam>
public abstract class ProcessCommandRunnerBase<TResult> : IDisposable
    where TResult : ProcessCommandResult
{
    private readonly ILogger _logger;
    private readonly Subject<string> _stdoutSubject = new();
    private readonly Subject<string> _stderrSubject = new();

    /// <summary>
    /// Standard output lines from the running command, streamed as they arrive.
    /// </summary>
    public IObservable<string> StdOut => _stdoutSubject;

    /// <summary>
    /// Standard error lines from the running command, streamed as they arrive.
    /// </summary>
    public IObservable<string> StdErr => _stderrSubject;

    /// <summary>The logger used for process execution messages.</summary>
    protected ILogger Logger => _logger;

    /// <summary>Initializes a new instance of <see cref="ProcessCommandRunnerBase{TResult}"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    protected ProcessCommandRunnerBase(ILogger logger)
    {
        _logger = logger.WhenNotNull();
    }

    /// <summary>Executes the specified command, streaming output and mapping the outcome to a typed result.</summary>
    /// <param name="command">The command or executable path to run.</param>
    /// <param name="arguments">The command-line arguments to pass to the process.</param>
    /// <param name="workingDirectory">The working directory for the process, or <see langword="null"/> for none.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <typeparamref name="TResult"/> describing the outcome of the command.</returns>
    protected async Task<TResult> RunAsync(string command, IReadOnlyList<string> arguments, string? workingDirectory,
        CancellationToken cancellationToken)
    {
        Logger.LogInformation(
            "Executing {OperationName}: {Command} {Arguments}",
            OperationName,
            command,
            string.Join(' ', arguments));

        var executorOptions = ProcessBuilder
            .For(command)
            .WithNoWindow();

        if (workingDirectory.IsNotNullOrEmpty())
        {
            executorOptions = executorOptions.WithWorkingDirectory(workingDirectory);
        }

        if (arguments.Count > 0)
        {
            executorOptions = executorOptions.WithArguments([.. arguments]);
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
                Logger.LogInformation("{OperationName} started.", OperationName);

                cancellationToken.ThrowIfCancellationRequested();

                var result = await executor.ExecuteAsync(cancellationToken).ConfigureAwait(false);

                Logger.LogInformation(
                    "{OperationName} completed with exit code {ExitCode}.",
                    OperationName,
                    result.ExitCode);

                var succeeded = result.ExitCode == 0;

                return CreateResult(succeeded, result.ExitCode, succeeded
                    ? null
                    : $"{OperationName} exited with code {result.ExitCode}.");
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("{OperationName} was cancelled.", OperationName);

                return CreateResult(false, CancelledExitCode, $"{OperationName} was cancelled.");
            }
            catch (TimeoutException exception)
            {
                Logger.LogError(exception, "{OperationName} timed out.", OperationName);

                return CreateResult(false, TimeoutExitCode, $"{OperationName} timed out.");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "{OperationName} failed with an unexpected error.", OperationName);

                return CreateResult(false, UnexpectedErrorExitCode, exception.Message);
            }
        }
    }

    /// <summary>Creates the concrete result from the process outcome.</summary>
    /// <param name="succeeded">Whether the command completed successfully.</param>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="errorMessage">An error message when the command failed, otherwise <see langword="null"/>.</param>
    /// <returns>The concrete result instance.</returns>
    protected abstract TResult CreateResult(bool succeeded, int exitCode, string? errorMessage);

    /// <summary>The user-facing operation name used in log and error messages.</summary>
    protected abstract string OperationName { get; }

    /// <summary>The exit code reported when the command is cancelled.</summary>
    protected abstract int CancelledExitCode { get; }

    /// <summary>The exit code reported when the command times out.</summary>
    protected abstract int TimeoutExitCode { get; }

    /// <summary>The exit code reported when an unexpected error occurs.</summary>
    protected abstract int UnexpectedErrorExitCode { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _stdoutSubject.Dispose();
        _stderrSubject.Dispose();

        GC.SuppressFinalize(this);
    }
}
