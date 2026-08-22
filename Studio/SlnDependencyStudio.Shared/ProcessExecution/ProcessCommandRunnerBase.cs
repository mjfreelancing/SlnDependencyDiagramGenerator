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
    private bool _disposed;

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
        _logger = logger;
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

                if (result.ExitCode == 0)
                {
                    Logger.LogInformation("{OperationName} completed successfully.", OperationName);

                    return CreateResult(CommandErrorCode.None, result.ExitCode, null);
                }

                Logger.LogError("{OperationName} exited with code {ExitCode}.", OperationName, result.ExitCode);

                return CreateResult(CommandErrorCode.ProcessExitedWithFailure, result.ExitCode,
                    $"{OperationName} exited with code {result.ExitCode}.");
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("{OperationName} was cancelled.", OperationName);

                return CreateResult(CommandErrorCode.Cancelled, null, $"{OperationName} was cancelled.");
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "{OperationName} failed with an unexpected error.", OperationName);

                return CreateResult(CommandErrorCode.UnexpectedError, null, exception.Message);
            }
        }
    }

    /// <summary>Creates the concrete result from the process outcome.</summary>
    /// <param name="errorCode">The failure classification; <see cref="CommandErrorCode.None"/> on success.</param>
    /// <param name="exitCode">The actual process exit code, or <see langword="null"/> when the process did not run to completion.</param>
    /// <param name="errorMessage">An error message when the command failed or was cancelled, otherwise <see langword="null"/>.</param>
    /// <returns>The concrete result instance.</returns>
    protected abstract TResult CreateResult(CommandErrorCode errorCode, int? exitCode, string? errorMessage);

    /// <summary>The user-facing operation name used in log and error messages.</summary>
    protected abstract string OperationName { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Complete the output subjects before disposing them so subscribers waiting for completion
        // (e.g. ToArray, LastAsync, aggregations) observe OnCompleted instead of hanging forever.
        _stdoutSubject.OnCompleted();
        _stderrSubject.OnCompleted();

        _stdoutSubject.Dispose();
        _stderrSubject.Dispose();

        GC.SuppressFinalize(this);
    }
}
