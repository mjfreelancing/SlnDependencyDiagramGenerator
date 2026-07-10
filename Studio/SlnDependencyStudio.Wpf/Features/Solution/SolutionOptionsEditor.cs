using DynamicData.Binding;
using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Disposables;

namespace SlnDependencyStudio.Wpf.Features.Solution;

/// <summary>
/// Reactive editing wrapper for <see cref="GeneratorSolutionOptions"/>.
/// </summary>
internal sealed class SolutionOptionsEditor : ReactiveObject, ISolutionOptionsEditor, IDisposable
{
    private readonly CompositeDisposable _disposables = [];
    private readonly ObservableCollectionExtended<string> _regexToIncludeCollection;
    private readonly ObservableCollectionExtended<string> _regexToExcludeCollection;
    private readonly ObservableCollectionExtended<string> _packagesToExcludeCollection;
    private readonly ObservableCollectionExtended<string> _frameworksToExcludeCollection;
    private string[] _originalRegexToInclude = [];
    private string[] _originalRegexToExclude = [];
    private string[] _originalPackagesToExclude = [];
    private string[] _originalFrameworksToExclude = [];
    private bool _isDirty;

    /// <inheritdoc />
    public TrackableValue<string> SolutionPath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<bool> UseRelativePath { get; } = new();

    /// <inheritdoc />
    public TrackableValue<ObservableCollection<string>> RegexToInclude { get; } = new();

    /// <inheritdoc />
    public TrackableValue<ObservableCollection<string>> RegexToExclude { get; } = new();

    /// <inheritdoc />
    public TrackableValue<ObservableCollection<string>> PackagesToExclude { get; } = new();

    /// <inheritdoc />
    public TrackableValue<ObservableCollection<string>> FrameworksToExclude { get; } = new();

    /// <inheritdoc />
    public bool IsDirty => _isDirty;

    /// <summary>Initializes a new instance of <see cref="SolutionOptionsEditor"/>.</summary>
    public SolutionOptionsEditor()
    {
        InitializeTrackable(SolutionPath, string.Empty);
        InitializeTrackable(UseRelativePath, true);

        _regexToIncludeCollection = [];
        _regexToExcludeCollection = [];
        _packagesToExcludeCollection = [];
        _frameworksToExcludeCollection = [];

        InitializeTrackable(RegexToInclude, _regexToIncludeCollection);
        InitializeTrackable(RegexToExclude, _regexToExcludeCollection);
        InitializeTrackable(PackagesToExclude, _packagesToExcludeCollection);
        InitializeTrackable(FrameworksToExclude, _frameworksToExcludeCollection);

        _regexToIncludeCollection.CollectionChanged += OnCollectionChanged;
        _regexToExcludeCollection.CollectionChanged += OnCollectionChanged;
        _packagesToExcludeCollection.CollectionChanged += OnCollectionChanged;
        _frameworksToExcludeCollection.CollectionChanged += OnCollectionChanged;

        SolutionPath.WhenAnyValue(path => path.IsDirty)
            .Subscribe(_ => UpdateIsDirty());
    }

    /// <summary>Populates all TrackableValues from the given options and marks the editor clean.</summary>
    /// <param name="source">The solution options to load.</param>
    public void SetOriginalValues(GeneratorSolutionOptions source)
    {
        SolutionPath.SetOriginalValue(source.SolutionPath);

        _originalRegexToInclude = source.RegexToInclude;
        _originalRegexToExclude = source.RegexToExclude;
        _originalPackagesToExclude = source.PackagesToExclude;
        _originalFrameworksToExclude = source.FrameworksToExclude;

        RegexToInclude.SetOriginalValue(_regexToIncludeCollection);
        RegexToExclude.SetOriginalValue(_regexToExcludeCollection);
        PackagesToExclude.SetOriginalValue(_packagesToExcludeCollection);
        FrameworksToExclude.SetOriginalValue(_frameworksToExcludeCollection);

        _regexToIncludeCollection.Load(source.RegexToInclude);
        _regexToExcludeCollection.Load(source.RegexToExclude);
        _packagesToExcludeCollection.Load(source.PackagesToExclude);
        _frameworksToExcludeCollection.Load(source.FrameworksToExclude);
    }

    /// <summary>Writes current TrackableValue contents back to the given options instance.</summary>
    /// <param name="target">The solution options to mutate.</param>
    public void FlushTo(GeneratorSolutionOptions target)
    {
        target.SolutionPath = SolutionPath.Value;
        target.RegexToInclude = [.. _regexToIncludeCollection];
        target.RegexToExclude = [.. _regexToExcludeCollection];
        target.PackagesToExclude = [.. _packagesToExcludeCollection];
        target.FrameworksToExclude = [.. _frameworksToExcludeCollection];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateIsDirty();
    }

    private void UpdateIsDirty()
    {
        _isDirty = SolutionPath.IsDirty ||
                   !_regexToIncludeCollection.OrderBy(item => item).SequenceEqual(_originalRegexToInclude.OrderBy(item => item)) ||
                   !_regexToExcludeCollection.OrderBy(item => item).SequenceEqual(_originalRegexToExclude.OrderBy(item => item)) ||
                   !_packagesToExcludeCollection.OrderBy(item => item).SequenceEqual(_originalPackagesToExclude.OrderBy(item => item)) ||
                   !_frameworksToExcludeCollection.OrderBy(item => item).SequenceEqual(_originalFrameworksToExclude.OrderBy(item => item));

        this.RaisePropertyChanged(nameof(IsDirty));
    }

    private void InitializeTrackable<TValue>(TrackableValue<TValue> trackable, TValue defaultValue = default!)
    {
        trackable.SetOriginalValue(defaultValue);
        _disposables.Add(trackable);
    }
}
