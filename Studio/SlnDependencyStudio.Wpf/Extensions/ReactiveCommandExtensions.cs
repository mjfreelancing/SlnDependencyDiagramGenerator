using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Extensions;

/// <summary>Extension methods for wiring ReactiveUI command exception handling.</summary>
internal static class ReactiveCommandExtensions
{
    /// <summary>Routes a command's unhandled exceptions to the error dialog service, so a failing user
    /// action shows a contextual error instead of reaching the global fallback (message box) handler.</summary>
    /// <param name="command">The command whose <c>ThrownExceptions</c> to subscribe to.</param>
    /// <param name="errorDialog">The error dialog service.</param>
    /// <param name="title">The dialog title describing the failed operation.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A disposable that detaches the subscription.</returns>
    public static IDisposable WireThrownExceptionsToErrorDialog(
        this IReactiveCommand command, IErrorDialogService errorDialog, string title, ILogger logger)
    {
        return command
            .ThrownExceptions
            .Subscribe(exception =>
            {
                try
                {
                    logger.LogError("{Title}: command failed: {ErrorMessage}", title, exception.Message);

                    errorDialog.ShowError
                        .Handle(new ErrorInfo(title, exception.Message))
                        .Subscribe(
                            _ => { },
                            dialogException => logger.LogError("Failed to show the error dialog for {Title}: {ErrorMessage}", title, dialogException.Message));
                }
                catch (Exception dialogException)
                {
                    // The error dialog needs a registered View handler (MainWindow); if it is missing, log
                    // the dialog failure - the global default handler still surfaces the original exception
                    // via a fallback message box.
                    logger.LogError("Failed to show the error dialog for {Title}: {ErrorMessage}", title, dialogException.Message);
                }
            });
    }
}
