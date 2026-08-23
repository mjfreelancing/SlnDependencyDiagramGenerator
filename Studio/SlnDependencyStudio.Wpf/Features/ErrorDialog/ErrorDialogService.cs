using ReactiveUI;
using System.Reactive;

namespace SlnDependencyStudio.Wpf.Features.ErrorDialog;

/// <summary>Default implementation of <see cref="IErrorDialogService"/>.</summary>
internal sealed class ErrorDialogService : IErrorDialogService
{
    /// <inheritdoc />
    public Interaction<ErrorInfo, Unit> ShowError { get; } = new();
}
