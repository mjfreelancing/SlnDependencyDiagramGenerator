using Microsoft.Extensions.Logging;
using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Solution;

/// <summary>
/// Reactive editing wrapper for <see cref="GeneratorSolutionOptions"/>.
/// </summary>
internal sealed class SolutionOptionsEditor : ReactiveObject, ISolutionOptionsEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ILogger<SolutionOptionsEditor> _logger;
    private bool _isDirty;

    /// <inheritdoc />
    public TrackableValue<string> SolutionPath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> UseRelativePath { get; } = new();

    /// <inheritdoc />
    public TrackableCollection<string> RegexToInclude { get; } = new();

    /// <inheritdoc />
    public TrackableCollection<string> RegexToExclude { get; } = new();

    /// <inheritdoc />
    public TrackableCollection<string> PackagesToExclude { get; } = new();

    /// <inheritdoc />
    public TrackableCollection<string> FrameworksToExclude { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> IndividualEnabled { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> IndividualIncludeDependencies { get; } = new();

    /// <inheritdoc />
    public TrackableValue<int> IndividualTransitiveDepth { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> AllEnabled { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> AllIncludeDependencies { get; } = new();

    /// <inheritdoc />
    public TrackableValue<int> AllTransitiveDepth { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty;

    /// <summary>Initializes a new instance of <see cref="SolutionOptionsEditor"/>.</summary>
    public SolutionOptionsEditor(ILogger<SolutionOptionsEditor> logger)
    {
        _logger = logger;

        InitializeTrackable(SolutionPath, string.Empty);
        InitializeTrackable(UseRelativePath, true);
        InitializeTrackable(IndividualEnabled, false);
        InitializeTrackable(IndividualIncludeDependencies, false);
        InitializeTrackable(IndividualTransitiveDepth, 0);
        InitializeTrackable(AllEnabled, false);
        InitializeTrackable(AllIncludeDependencies, false);
        InitializeTrackable(AllTransitiveDepth, 0);

        _disposables.Add(RegexToInclude);
        _disposables.Add(RegexToExclude);
        _disposables.Add(PackagesToExclude);
        _disposables.Add(FrameworksToExclude);

        var collectionDirtyFlags = new IObservable<bool>[]
        {
            RegexToInclude.WhenAnyValue(trackable => trackable.IsDirty),
            RegexToExclude.WhenAnyValue(trackable => trackable.IsDirty),
            PackagesToExclude.WhenAnyValue(trackable => trackable.IsDirty),
            FrameworksToExclude.WhenAnyValue(trackable => trackable.IsDirty)
        };

        var trackableDirtyFlags = new[]
        {
            SolutionPath.WhenAnyValue(trackable => trackable.IsDirty),
            IndividualEnabled.WhenAnyValue(trackable => trackable.IsDirty),
            IndividualIncludeDependencies.WhenAnyValue(trackable => trackable.IsDirty),
            IndividualTransitiveDepth.WhenAnyValue(trackable => trackable.IsDirty),
            AllEnabled.WhenAnyValue(trackable => trackable.IsDirty),
            AllIncludeDependencies.WhenAnyValue(trackable => trackable.IsDirty),
            AllTransitiveDepth.WhenAnyValue(trackable => trackable.IsDirty)
        };

        var subscription = Observable
            .CombineLatest([.. trackableDirtyFlags, .. collectionDirtyFlags])
            .Subscribe(_ => UpdateIsDirty());

        _disposables.Add(subscription);
    }

    /// <summary>Populates all TrackableValues from the given options and marks the editor clean.</summary>
    /// <param name="source">The solution options to load.</param>
    public void SetOriginalValues(GeneratorSolutionOptions source)
    {
        _logger.LogDebug("Set original values on {Editor}", nameof(SolutionOptionsEditor));

        SolutionPath.SetOriginalValue(source.SolutionPath);

        RegexToInclude.SetOriginalItems(source.RegexToInclude);
        RegexToExclude.SetOriginalItems(source.RegexToExclude);
        PackagesToExclude.SetOriginalItems(source.PackagesToExclude);
        FrameworksToExclude.SetOriginalItems(source.FrameworksToExclude);

        IndividualEnabled.SetOriginalValue(source.Individual.Enabled);
        IndividualIncludeDependencies.SetOriginalValue(source.Individual.IncludeDependencies);
        IndividualTransitiveDepth.SetOriginalValue(source.Individual.TransitiveDepth);
        AllEnabled.SetOriginalValue(source.All.Enabled);
        AllIncludeDependencies.SetOriginalValue(source.All.IncludeDependencies);
        AllTransitiveDepth.SetOriginalValue(source.All.TransitiveDepth);
    }

    /// <summary>Writes current TrackableValue contents back to the given options instance.</summary>
    /// <param name="target">The solution options to mutate.</param>
    public void FlushTo(GeneratorSolutionOptions target)
    {
        target.SolutionPath = SolutionPath.Value;
        target.RegexToInclude = RegexToInclude.ToArray();
        target.RegexToExclude = RegexToExclude.ToArray();
        target.PackagesToExclude = PackagesToExclude.ToArray();
        target.FrameworksToExclude = FrameworksToExclude.ToArray();

        target.Individual.Enabled = IndividualEnabled.Value;
        target.Individual.IncludeDependencies = IndividualIncludeDependencies.Value;
        target.Individual.TransitiveDepth = IndividualTransitiveDepth.Value;
        target.All.Enabled = AllEnabled.Value;
        target.All.IncludeDependencies = AllIncludeDependencies.Value;
        target.All.TransitiveDepth = AllTransitiveDepth.Value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void UpdateIsDirty()
    {
        _isDirty = SolutionPath.IsDirty ||
                   RegexToInclude.IsDirty ||
                   RegexToExclude.IsDirty ||
                   PackagesToExclude.IsDirty ||
                   FrameworksToExclude.IsDirty ||
                   IndividualEnabled.IsDirty ||
                   IndividualIncludeDependencies.IsDirty ||
                   IndividualTransitiveDepth.IsDirty ||
                   AllEnabled.IsDirty ||
                   AllIncludeDependencies.IsDirty ||
                   AllTransitiveDepth.IsDirty;

        this.RaisePropertyChanged(nameof(IsDirty));
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }
}
