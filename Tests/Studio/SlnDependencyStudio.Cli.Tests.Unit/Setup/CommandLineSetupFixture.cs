using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Cli.Handlers.Validate;
using SlnDependencyStudio.Cli.Setup;
using System.CommandLine;

namespace SlnDependencyStudio.Cli.Tests.Unit.Setup;

public class CommandLineSetupFixture
{
    [Fact]
    public void Build_Should_Include_Validate_Command()
    {
        var setup = new CommandLineSetup();

        var root = setup
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        root.Children.Any(child => child is Command cmd && cmd.Name == "validate").ShouldBeTrue();
    }

    [Fact]
    public void Build_Should_Include_Run_Command()
    {
        var setup = new CommandLineSetup();

        var root = setup
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        root.Children.Any(child => child is Command cmd && cmd.Name == "run").ShouldBeTrue();
    }

    [Fact]
    public void Build_Should_Include_ProjectFile_Option_On_Root()
    {
        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // Verify --pf parses at the root level (reachable without a subcommand)
        var parseResult = root.Parse("--pf file.sds");
        parseResult.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Each_Command_Should_Have_ProjectFile_Option()
    {
        var handler = Substitute.For<ICommandLineRunHandler>();

        handler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));

        var root = new CommandLineSetup()
            .AddRun(handler, _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // Verify --pf is recognised under the run subcommand
        var parseResult = root.Parse("run --pf file.sds");
        parseResult.Errors.ShouldBeEmpty();

        await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);
        await handler.Received(1).HandleAsync("file.sds", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_Command_Should_Invoke_Handler_With_ProjectFile()
    {
        var handler = Substitute.For<ICommandLineValidateHandler>();
        var exitCode = 0;

        handler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(99));

        var root = new CommandLineSetup()
            .AddValidate(handler, code => exitCode = code)
            .Build(Substitute.For<ILogger>(), out _);

        var parseResult = root.Parse("validate --pf other.sds");
        await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        await handler.Received(1).HandleAsync("other.sds", Arg.Any<CancellationToken>());
        exitCode.ShouldBe(99);
    }

    [Fact]
    public async Task Run_Command_Should_Invoke_Handler_With_ProjectFile()
    {
        var handler = Substitute.For<ICommandLineRunHandler>();
        var exitCode = 0;

        handler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(42));

        var root = new CommandLineSetup()
            .AddRun(handler, code => exitCode = code)
            .Build(Substitute.For<ILogger>(), out _);

        var parseResult = root.Parse("run --pf test.sds");
        await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        await handler.Received(1).HandleAsync("test.sds", Arg.Any<CancellationToken>());
        exitCode.ShouldBe(42);
    }

    [Fact]
    public async Task Handler_Should_Receive_The_Invocation_CancellationToken()
    {
        var handler = Substitute.For<ICommandLineRunHandler>();
        using var cts = new CancellationTokenSource();

        CancellationToken receivedToken = default;

        handler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                receivedToken = callInfo.Arg<CancellationToken>();
                return Task.FromResult(0);
            });

        var root = new CommandLineSetup()
            .AddRun(handler, _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        var parseResult = root.Parse("run --pf file.sds");

        // Cancel before invoking: the token-aware SetAction forwards the invocation's cancellation token
        // (System.CommandLine links it to the token passed to InvokeAsync), so the handler observes the
        // cancellation rather than a fresh token captured via a closure field.
        cts.Cancel();

        await parseResult.InvokeAsync(cancellationToken: cts.Token);

        await handler.Received(1).HandleAsync("file.sds", Arg.Any<CancellationToken>());
        receivedToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task Unknown_Command_Should_Fall_Through_To_Root_Action()
    {
        var logger = Substitute.For<ILogger>();

        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(logger, out _);

        var parseResult = root.Parse("--pf test.sds");
        await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        logger.Received(1).Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Is<object>(obj => obj.ToString()!.Contains("run")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Build_Should_Return_VerboseOption()
    {
        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out var verboseOption);

        verboseOption.ShouldNotBeNull();
        verboseOption.Aliases.ShouldContain("-v");
    }

    [Fact]
    public void Validate_Command_Should_Accept_Verbose_Flag()
    {
        var root = new CommandLineSetup()
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // Short form
        root.Parse("validate -v --pf file.sds").Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Run_Command_Should_Accept_Verbose_Flag()
    {
        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // Long form
        root.Parse("run --verbose --pf file.sds").Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Verbose_Flag_Should_Be_Accepted_Before_Subcommand()
    {
        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // --verbose is recursive on the root, so the global position (before the subcommand) parses too (CL-L2).
        root.Parse("--verbose run --pf file.sds").Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Bare_Invocation_Should_Fall_Through_To_Root_Action()
    {
        var logger = Substitute.For<ILogger>();

        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(logger, out _);

        // A bare invocation (no --projectFile, no subcommand) must parse cleanly - the root copy of
        // --projectFile is not required - so the friendly root fallback fires (CL-L1) instead of a terse
        // "Option '--projectFile' is required." parse error.
        var parseResult = root.Parse("");
        parseResult.Errors.ShouldBeEmpty();

        var exitCode = await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        exitCode.ShouldBe((int)StudioCliExitCode.CommandLineParseFailed);

        logger.Received(1).Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Error),
            Arg.Any<EventId>(),
            Arg.Is<object>(obj => obj.ToString()!.Contains("run")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void Run_Command_Should_Require_ProjectFile()
    {
        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // --projectFile is required per-subcommand, so omitting it on `run` still reports the standard error.
        root.Parse("run").Errors
            .Select(error => error.Message)
            .ShouldContain("Option '--projectFile' is required.");
    }

    [Fact]
    public void Validate_Command_Should_Require_ProjectFile()
    {
        var root = new CommandLineSetup()
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // --projectFile is required per-subcommand, so omitting it on `validate` still reports the standard error.
        root.Parse("validate").Errors
            .Select(error => error.Message)
            .ShouldContain("Option '--projectFile' is required.");
    }
}
