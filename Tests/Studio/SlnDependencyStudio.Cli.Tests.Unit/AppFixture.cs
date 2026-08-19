using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog.Core;
using Shouldly;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Handlers.Run;
using SlnDependencyStudio.Cli.Handlers.Validate;

namespace SlnDependencyStudio.Cli.Tests.Unit;

public class AppFixture
{
    [Fact]
    public async Task Should_Return_UserCancelled_When_Token_Is_Cancelled_During_Command()
    {
        using var cts = new CancellationTokenSource();

        var runHandler = Substitute.For<ICommandLineRunHandler>();

        runHandler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var token = callInfo.Arg<CancellationToken>();

                // Simulate Ctrl+C while the command is in flight by cancelling the token passed to StartAsync.
                cts.Cancel();

                // AllOverIt.GenericHost hands StartAsync a token linked against ApplicationStopping, so a
                // shutdown request cancels it. The handler observes that cancellation (as the real handlers
                // do) and the operation surfaces as an OCE, which App maps to UserCancelled.
                token.IsCancellationRequested.ShouldBeTrue();

                return Task.FromException<int>(new OperationCanceledException());
            });

        var app = new App(
            CreateScopeFactory(Substitute.For<ICommandLineValidateHandler>(), runHandler),
            new LoggingLevelSwitch(),
            Substitute.For<ILogger<App>>());

        await app.StartAsync(["run", "--cf", @"C:\tmp\config.sds"], cts.Token);

        // A user cancellation is distinct from a failure and must not exit 0.
        app.ExitCode.ShouldBe((int)StudioCliExitCode.UserCancelled);
    }

    [Fact]
    public async Task Should_Return_OperationCancelled_When_Oce_Is_Thrown_Without_User_Cancellation()
    {
        var runHandler = Substitute.For<ICommandLineRunHandler>();

        runHandler
            .HandleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var app = new App(
            CreateScopeFactory(Substitute.For<ICommandLineValidateHandler>(), runHandler),
            new LoggingLevelSwitch(),
            Substitute.For<ILogger<App>>());

        await app.StartAsync(["run", "--cf", @"C:\tmp\config.sds"], CancellationToken.None);

        // An OCE that is not caused by the user's shutdown token is an internal operation cancellation,
        // distinct from a user-requested shutdown.
        app.ExitCode.ShouldBe((int)StudioCliExitCode.OperationCancelled);
    }

    [Fact]
    public async Task Should_Not_Throw_And_Return_ParseFailed_When_Invoked_With_No_Arguments()
    {
        var app = new App(
            CreateScopeFactory(Substitute.For<ICommandLineValidateHandler>(), Substitute.For<ICommandLineRunHandler>()),
            new LoggingLevelSwitch(),
            Substitute.For<ILogger<App>>());

        // A bare `studio` invocation (no --configFile, no subcommand) must surface as a parse error
        // (exit 1001), not throw InvalidOperationException out of StartAsync — a throw would be
        // reported by the host as an "Unhandled exception!" stack trace with exit code -1.
        await app.StartAsync([], CancellationToken.None);

        app.ExitCode.ShouldBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    private static IServiceScopeFactory CreateScopeFactory(
        ICommandLineValidateHandler validateHandler, ICommandLineRunHandler runHandler)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICommandLineValidateHandler)).Returns(validateHandler);
        serviceProvider.GetService(typeof(ICommandLineRunHandler)).Returns(runHandler);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return scopeFactory;
    }
}
