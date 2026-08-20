using NSubstitute;
using ReactiveUI;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Support;

/// <summary>Helpers for verifying that a command wired via <c>WireThrownExceptionsToErrorDialog</c> surfaces
/// its failure through the error dialog interaction. Used so removing the wiring (or breaking the exception
/// path) fails a test.</summary>
internal static class ErrorDialogTestHelpers
{
    /// <summary>Creates an <see cref="IErrorDialogService"/> substitute whose <see cref="IErrorDialogService.ShowError"/>
    /// interaction is a real interaction, so a test can register a handler to capture the <see cref="ErrorInfo"/>.</summary>
    public static IErrorDialogService CreateErrorDialogSubstitute(out Interaction<ErrorInfo, System.Reactive.Unit> interaction)
    {
        interaction = new();

        var substitute = Substitute.For<IErrorDialogService>();

        substitute.ShowError.Returns(interaction);

        return substitute;
    }

    /// <summary>Triggers a command execution without asserting on its outcome. Failures are surfaced through the
    /// command's wired ThrownExceptions handler (asserted via the error dialog interaction the test awaits); both
    /// callbacks here are no-ops so an execution error cannot crash the test runner.</summary>
    public static Task ObserveExecuteIgnoringOutcomeAsync<TParam, TResult>(
        ReactiveCommand<TParam, TResult> command, TParam parameter)
    {
        command.Execute(parameter).Subscribe(_ => { }, _ => { });

        return Task.CompletedTask;
    }

    /// <summary>Awaits the captured error with a timeout so that if the wiring is ever removed (and the
    /// error dialog never fires), the test fails with a timeout rather than hanging the test runner.</summary>
    public static async Task<ErrorInfo> WaitForCapturedErrorAsync(Task<ErrorInfo> capturedErrorTask)
    {
        return await capturedErrorTask.WaitAsync(TimeSpan.FromSeconds(5));
    }
}
