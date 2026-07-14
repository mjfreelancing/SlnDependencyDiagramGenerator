using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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
    public TrackableValue<GeneratorDiagramOptions.DiagramDirection> Direction { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> FrameworkFill { get; } = new();

    /// <inheritdoc />
    public TrackableValue<double> FrameworkOpacity { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> PackageFill { get; } = new();

    /// <inheritdoc />
    public TrackableValue<double> PackageOpacity { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> TransitiveFill { get; } = new();

    /// <inheritdoc />
    public TrackableValue<double> TransitiveOpacity { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> GroupingEnabled { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> GroupingFill { get; } = new();

    /// <inheritdoc />
    public TrackableValue<double> GroupingOpacity { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> GroupName { get; } = new();

    /// <inheritdoc />
    public TrackableValue<string> GroupNameAlias { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty;

    /// <summary>Initializes a new instance with empty defaults.</summary>
    public DiagramOptionsEditor()
    {
        _disposables.Add(Formats);

        InitializeTrackable(Direction, GeneratorDiagramOptions.DiagramDirection.LR);
        InitializeTrackable(FrameworkFill, string.Empty);
        InitializeTrackable(FrameworkOpacity, 1.0);
        InitializeTrackable(PackageFill, string.Empty);
        InitializeTrackable(PackageOpacity, 1.0);
        InitializeTrackable(TransitiveFill, string.Empty);
        InitializeTrackable(TransitiveOpacity, 1.0);
        InitializeTrackable(GroupingEnabled, true);
        InitializeTrackable(GroupingFill, "#E7EBFC");
        InitializeTrackable(GroupingOpacity, 1.0);
        InitializeTrackable(GroupName, string.Empty);
        InitializeTrackable(GroupNameAlias, string.Empty);

        var dirtyFlags = new IObservable<bool>[]
        {
            Formats.WhenAnyValue(formats => formats.IsDirty),
            Direction.WhenAnyValue(direction => direction.IsDirty),
            FrameworkFill.WhenAnyValue(frameworkFill => frameworkFill.IsDirty),
            FrameworkOpacity.WhenAnyValue(frameworkOpacity => frameworkOpacity.IsDirty),
            PackageFill.WhenAnyValue(packageFill => packageFill.IsDirty),
            PackageOpacity.WhenAnyValue(packageOpacity => packageOpacity.IsDirty),
            TransitiveFill.WhenAnyValue(transitiveFill => transitiveFill.IsDirty),
            TransitiveOpacity.WhenAnyValue(transitiveOpacity => transitiveOpacity.IsDirty),
            GroupingEnabled.WhenAnyValue(groupingEnabled => groupingEnabled.IsDirty),
            GroupingFill.WhenAnyValue(groupingFill => groupingFill.IsDirty),
            GroupingOpacity.WhenAnyValue(groupingOpacity => groupingOpacity.IsDirty),
            GroupName.WhenAnyValue(groupName => groupName.IsDirty),
            GroupNameAlias.WhenAnyValue(groupNameAlias => groupNameAlias.IsDirty)
        };

        var subscription = Observable
            .CombineLatest(dirtyFlags)
            .Subscribe(_ => UpdateIsDirty());

        _disposables.Add(subscription);
    }

    /// <inheritdoc />
    public void SetOriginalValues(GeneratorDiagramOptions source)
    {
        Formats.SetOriginalItems(source.Formats);

        Direction.SetOriginalValue(source.Direction);
        FrameworkFill.SetOriginalValue(source.FrameworkStyle.Fill);
        FrameworkOpacity.SetOriginalValue(source.FrameworkStyle.Opacity);
        PackageFill.SetOriginalValue(source.PackageStyle.Fill);
        PackageOpacity.SetOriginalValue(source.PackageStyle.Opacity);
        TransitiveFill.SetOriginalValue(source.TransitiveStyle.Fill);
        TransitiveOpacity.SetOriginalValue(source.TransitiveStyle.Opacity);
        GroupingEnabled.SetOriginalValue(source.Grouping.Enabled);
        GroupingFill.SetOriginalValue(source.Grouping.BackgroundStyle.Fill);
        GroupingOpacity.SetOriginalValue(source.Grouping.BackgroundStyle.Opacity);
        GroupName.SetOriginalValue(source.GroupName);
        GroupNameAlias.SetOriginalValue(source.GroupNameAlias);
    }

    /// <inheritdoc />
    public void FlushTo(GeneratorDiagramOptions target)
    {
        target.Formats = Formats.ToArray();

        target.Direction = Direction.Value;
        target.FrameworkStyle.Fill = FrameworkFill.Value;
        target.FrameworkStyle.Opacity = FrameworkOpacity.Value;
        target.PackageStyle.Fill = PackageFill.Value;
        target.PackageStyle.Opacity = PackageOpacity.Value;
        target.TransitiveStyle.Fill = TransitiveFill.Value;
        target.TransitiveStyle.Opacity = TransitiveOpacity.Value;
        target.Grouping.Enabled = GroupingEnabled.Value;
        target.Grouping.BackgroundStyle.Fill = GroupingFill.Value;
        target.Grouping.BackgroundStyle.Opacity = GroupingOpacity.Value;
        target.GroupName = GroupName.Value;
        target.GroupNameAlias = GroupNameAlias.Value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void UpdateIsDirty()
    {
        _isDirty = Formats.IsDirty ||
                   Direction.IsDirty ||
                   FrameworkFill.IsDirty ||
                   FrameworkOpacity.IsDirty ||
                   PackageFill.IsDirty ||
                   PackageOpacity.IsDirty ||
                   TransitiveFill.IsDirty ||
                   TransitiveOpacity.IsDirty ||
                   GroupingEnabled.IsDirty ||
                   GroupingFill.IsDirty ||
                   GroupingOpacity.IsDirty ||
                   GroupName.IsDirty ||
                   GroupNameAlias.IsDirty;

        this.RaisePropertyChanged(nameof(IsDirty));
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }
}
