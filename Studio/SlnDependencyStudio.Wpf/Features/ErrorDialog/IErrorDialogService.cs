using ReactiveUI;
using SlnDependencyStudio.Shared.DependencyInjection;
using System.Reactive;

namespace SlnDependencyStudio.Wpf.Features.ErrorDialog;

/// <summary>Service for displaying error messages to the user via a modal dialog.
/// Consumed by ViewModels; the View registers the dialog handler.</summary>
public interface IErrorDialogService : IStudioSingletonDependency
{
    /// <summary>Interaction that displays an error dialog. The View registers a handler
    /// that shows a Material Design dialog; the ViewModel calls <c>Handle</c> with the error details.</summary>
    Interaction<ErrorInfo, Unit> ShowError { get; }
}
