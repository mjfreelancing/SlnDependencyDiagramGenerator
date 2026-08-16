using AllOverIt.Assertion;
using AllOverIt.Extensions;
using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Utils;

namespace SlnDependencyStudio.Shared.ProcessExecution.PostGeneration;

/// <inheritdoc cref="IPostGenerationCommandRunner"/>
internal sealed class PostGenerationCommandRunner : ProcessCommandRunnerBase<PostGenerationCommandResult>, IPostGenerationCommandRunner
{
    /// <inheritdoc />
    protected override PostGenerationCommandResult CreateResult(CommandErrorCode errorCode, int? exitCode, string? errorMessage)
        => new()
        {
            ErrorCode = errorCode,
            ExitCode = exitCode,
            ErrorMessage = errorMessage
        };

    /// <inheritdoc />
    protected override string OperationName => "Post-generation command";

    /// <summary>Initializes a new instance of <see cref="PostGenerationCommandRunner"/>.</summary>
    /// <param name="logger">The logger instance.</param>
    public PostGenerationCommandRunner(ILogger<PostGenerationCommandRunner> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    public async Task<PostGenerationCommandResult> RunAsync(PostGenerationConfig config, CancellationToken cancellationToken)
    {
        _ = config.WhenNotNull();

        if (!config.Enabled)
        {
            return new PostGenerationCommandResult
            {
                ExitCode = 0
            };
        }

        // Split the arguments string so each token is passed as a separate argument to the process.
        // The splitter honours double-quoted segments, so a value containing spaces stays a single
        // argument (e.g. --file "my file.txt" -> ["--file", "my file.txt"]).
        var arguments = config.Arguments.IsNotNullOrEmpty()
            ? CommandLineUtils.SplitArguments(config.Arguments)
            : [];

        return await RunAsync(config.Command, arguments, config.WorkingDirectory, cancellationToken).ConfigureAwait(false);
    }
}
