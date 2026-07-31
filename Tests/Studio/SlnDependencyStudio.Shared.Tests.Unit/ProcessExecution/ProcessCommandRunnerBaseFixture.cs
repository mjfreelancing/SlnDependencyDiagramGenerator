using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using SlnDependencyStudio.Shared.ProcessExecution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SlnDependencyStudio.Shared.Tests.Unit.ProcessExecution;

/// <summary>
/// Tests the shared <see cref="ProcessCommandRunnerBase{TResult}"/> behaviour using a test double.
/// Covers success, non-zero failure, pre-cancellation, unexpected start errors and output streaming.
/// </summary>
public class ProcessCommandRunnerBaseFixture
{
    public class Success : ProcessCommandRunnerBaseFixture
    {
        [Fact]
        public async Task Should_Succeed_When_Command_Exits_Zero()
        {
            using var runner = CreateRunner();

            var result = await runner.RunAsync("cmd.exe", ["/c", "exit 0"], null, CancellationToken.None);

            result.Succeeded.ShouldBeTrue();
            result.ExitCode.ShouldBe(0);
            result.ErrorMessage.ShouldBeNull();
        }
    }

    public class Failure : ProcessCommandRunnerBaseFixture
    {
        [Fact]
        public async Task Should_Fail_With_Actual_Exit_Code_When_Command_Exits_NonZero()
        {
            using var runner = CreateRunner();

            var result = await runner.RunAsync("cmd.exe", ["/c", "exit 42"], null, CancellationToken.None);

            result.Succeeded.ShouldBeFalse();
            result.ExitCode.ShouldBe(42);
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
        }
    }

    public class Cancellation : ProcessCommandRunnerBaseFixture
    {
        [Fact]
        public async Task Should_Report_Cancelled_Exit_Code_When_Token_Is_PreCancelled()
        {
            using var runner = CreateRunner();

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await runner.RunAsync("cmd.exe", ["/c", "echo should not run"], null, cts.Token);

            result.Succeeded.ShouldBeFalse();
            result.ExitCode.ShouldBe(TestExitCodes.Cancelled);
        }
    }

    public class UnexpectedError : ProcessCommandRunnerBaseFixture
    {
        [Fact]
        public async Task Should_Report_Unexpected_Error_When_Command_Cannot_Start()
        {
            using var runner = CreateRunner();

            var result = await runner.RunAsync(
                "this-command-does-not-exist-xyz",
                [],
                null,
                CancellationToken.None);

            result.Succeeded.ShouldBeFalse();
            result.ExitCode.ShouldBe(TestExitCodes.UnexpectedError);
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
        }
    }

    public class OutputStreaming : ProcessCommandRunnerBaseFixture
    {
        [Fact]
        public async Task Should_Stream_Standard_Output()
        {
            using var runner = CreateRunner();

            var lines = new List<string>();
            using var subscription = runner.StdOut.Subscribe(lines.Add);

            await runner.RunAsync("cmd.exe", ["/c", "echo hello-from-stdout"], null, CancellationToken.None);

            await WaitUntilAsync(() => lines.Any(line => line.Contains("hello-from-stdout")));

            lines.ShouldContain(line => line.Contains("hello-from-stdout"));
        }

        [Fact]
        public async Task Should_Stream_Standard_Error()
        {
            using var runner = CreateRunner();

            var lines = new List<string>();
            using var subscription = runner.StdErr.Subscribe(lines.Add);

            await runner.RunAsync("cmd.exe", ["/c", "echo hello-from-stderr 1>&2"], null, CancellationToken.None);

            await WaitUntilAsync(() => lines.Any(line => line.Contains("hello-from-stderr")));

            lines.ShouldContain(line => line.Contains("hello-from-stderr"));
        }
    }

    internal static class TestExitCodes
    {
        public const int Cancelled = 100;
        public const int Timeout = 101;
        public const int UnexpectedError = 102;
    }

    private static TestCommandRunner CreateRunner()
    {
        return new TestCommandRunner(
            NullLogger.Instance,
            TestExitCodes.Cancelled,
            TestExitCodes.Timeout,
            TestExitCodes.UnexpectedError);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));

        while (!predicate() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(25);
        }
    }

    internal sealed class TestCommandResult : ProcessCommandResult
    {
    }

    private sealed class TestCommandRunner : ProcessCommandRunnerBase<TestCommandResult>
    {
        public TestCommandRunner(ILogger logger, int cancelledExitCode, int timeoutExitCode, int unexpectedErrorExitCode)
            : base(logger)
        {
            CancelledExitCode = cancelledExitCode;
            TimeoutExitCode = timeoutExitCode;
            UnexpectedErrorExitCode = unexpectedErrorExitCode;
        }

        public new Task<TestCommandResult> RunAsync(string command, IReadOnlyList<string> arguments, string? workingDirectory,
            CancellationToken cancellationToken)
            => base.RunAsync(command, arguments, workingDirectory, cancellationToken);

        protected override TestCommandResult CreateResult(bool succeeded, int exitCode, string? errorMessage)
            => new()
            {
                Succeeded = succeeded,
                ExitCode = exitCode,
                ErrorMessage = errorMessage
            };

        protected override string OperationName => "Test command";

        protected override int CancelledExitCode { get; }

        protected override int TimeoutExitCode { get; }

        protected override int UnexpectedErrorExitCode { get; }
    }
}
