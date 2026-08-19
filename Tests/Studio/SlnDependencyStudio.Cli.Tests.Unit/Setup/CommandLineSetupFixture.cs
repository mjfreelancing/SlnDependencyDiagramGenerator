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
    public void Build_Should_Include_ConfigFile_Option_On_Root()
    {
        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // Verify --cf parses at the root level (reachable without a subcommand)
        var parseResult = root.Parse("--cf file.sds");
        parseResult.Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Each_Command_Should_Have_ConfigFile_Option()
    {
        var handler = Substitute.For<ICommandLineRunHandler>();

        handler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));

        var root = new CommandLineSetup()
            .AddRun(handler, _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // Verify --cf is recognised under the run subcommand
        var parseResult = root.Parse("run --cf file.sds");
        parseResult.Errors.ShouldBeEmpty();

        await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);
        await handler.Received(1).HandleAsync("file.sds", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_Command_Should_Invoke_Handler_With_ConfigFile()
    {
        var handler = Substitute.For<ICommandLineValidateHandler>();
        var exitCode = 0;

        handler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(99));

        var root = new CommandLineSetup()
            .AddValidate(handler, code => exitCode = code)
            .Build(Substitute.For<ILogger>(), out _);

        var parseResult = root.Parse("validate --cf other.sds");
        await parseResult.InvokeAsync(cancellationToken: TestContext.Current.CancellationToken);

        await handler.Received(1).HandleAsync("other.sds", Arg.Any<CancellationToken>());
        exitCode.ShouldBe(99);
    }

    [Fact]
    public async Task Run_Command_Should_Invoke_Handler_With_ConfigFile()
    {
        var handler = Substitute.For<ICommandLineRunHandler>();
        var exitCode = 0;

        handler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(42));

        var root = new CommandLineSetup()
            .AddRun(handler, code => exitCode = code)
            .Build(Substitute.For<ILogger>(), out _);

        var parseResult = root.Parse("run --cf test.sds");
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

        var parseResult = root.Parse("run --cf file.sds");

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

        var parseResult = root.Parse("--cf test.sds");
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
        root.Parse("validate -v --cf file.sds").Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Run_Command_Should_Accept_Verbose_Flag()
    {
        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // Long form
        root.Parse("run --verbose --cf file.sds").Errors.ShouldBeEmpty();
    }

    [Fact]
    public async Task Bare_Invocation_Should_Fall_Through_To_Root_Action()
    {
        var logger = Substitute.For<ILogger>();

        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(logger, out _);

        // A bare invocation (no --configFile, no subcommand) must parse cleanly - the root copy of
        // --configFile is not required - so the friendly root fallback fires (CL-L1) instead of a terse
        // "Option '--configFile' is required." parse error.
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
    public void Run_Command_Should_Require_ConfigFile()
    {
        var root = new CommandLineSetup()
            .AddRun(Substitute.For<ICommandLineRunHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // --configFile is required per-subcommand, so omitting it on `run` still reports the standard error.
        root.Parse("run").Errors
            .Select(error => error.Message)
            .ShouldContain("Option '--configFile' is required.");
    }

    [Fact]
    public void Validate_Command_Should_Require_ConfigFile()
    {
        var root = new CommandLineSetup()
            .AddValidate(Substitute.For<ICommandLineValidateHandler>(), _ => { })
            .Build(Substitute.For<ILogger>(), out _);

        // --configFile is required per-subcommand, so omitting it on `validate` still reports the standard error.
        root.Parse("validate").Errors
            .Select(error => error.Message)
            .ShouldContain("Option '--configFile' is required.");
    }
}
