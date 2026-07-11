using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;

namespace SlnDependencyStudio.Wpf.Features.Diagrams;

/// <summary>
/// Reactive editing wrapper for <see cref="GeneratorDiagramOptions"/>.
/// </summary>
internal sealed class DiagramOptionsEditor : ReactiveObject, IDiagramOptionsEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private bool _isDirty;

    /// <inheritdoc />
    public TrackableCollection<DiagramFormat> Formats { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty;

    /// <summary>Initializes a new instance with empty defaults.</summary>
    public DiagramOptionsEditor()
    {
        _disposables.Add(Formats);

        var subscription = Formats.WhenAnyValue(f => f.IsDirty)
            .Subscribe(_ => UpdateIsDirty());

        _disposables.Add(subscription);
    }

    /// <inheritdoc />
    public void SetOriginalValues(GeneratorDiagramOptions source)
    {
        Formats.SetOriginalItems(source.Formats);
    }

    /// <inheritdoc />
    public void FlushTo(GeneratorDiagramOptions target)
    {
        target.Formats = Formats.ToArray();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void UpdateIsDirty()
    {
        _isDirty = Formats.IsDirty;
        this.RaisePropertyChanged(nameof(IsDirty));
    }
}
