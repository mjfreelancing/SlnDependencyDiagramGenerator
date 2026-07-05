using ReactiveUI;
using System.Reactive;

namespace SlnDependencyStudio.Wpf.Features.ErrorDialog;

internal sealed class ErrorDialogService : IErrorDialogService
{
    /// <inheritdoc />
    public Interaction<ErrorInfo, Unit> ShowError { get; } = new();
}
