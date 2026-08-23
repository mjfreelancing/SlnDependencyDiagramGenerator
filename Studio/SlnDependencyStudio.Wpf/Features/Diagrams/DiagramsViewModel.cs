using AllOverIt.Extensions;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Diagrams.Models;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace SlnDependencyStudio.Wpf.Features.Diagrams;

/// <summary>View model for the "Diagrams" navigation page. Delegates all document-related
/// bindings to the <see cref="IProjectDocumentStore"/> and manages format toggle state.</summary>
public sealed partial class DiagramsViewModel : ReactiveObject, IValidatableViewModel, IDisposable
{
    [GeneratedRegex(@"^#?[0-9A-Fa-f]{6}$")]
    private static partial Regex HexPatternRegex();

    private readonly IProjectDocumentStore _store;
    private readonly CompositeDisposable _disposables = [];

    // Guards against infinite recursion between toggle changes and collection sync.
    // Toggling a checkbox modifies the Formats collection, which fires CollectionChanged,
    // which syncs toggle states — without this flag, each sync would re-trigger the
    // toggle-change handler. The two-way sync is inherently circular; a guard flag is
    // the standard pattern when neither side can be made a pure projection of the other.
    private bool _isSyncing;

    /// <summary>The diagram formats collection. Items are added/removed as toggles change.</summary>
    public TrackableCollection<DiagramFormat> Formats => _store.DiagramOptionsEditor.Formats;

    /// <summary>Diagram flow direction.</summary>
    public TrackableValue<GeneratorDiagramOptions.DiagramDirection> Direction => _store.DiagramOptionsEditor.Direction;

    /// <summary>Framework fill color.</summary>
    public TrackableValue<string> FrameworkFill => _store.DiagramOptionsEditor.FrameworkFill;

    /// <summary>Framework fill opacity.</summary>
    public TrackableValue<double> FrameworkOpacity => _store.DiagramOptionsEditor.FrameworkOpacity;

    /// <summary>Package fill color.</summary>
    public TrackableValue<string> PackageFill => _store.DiagramOptionsEditor.PackageFill;

    /// <summary>Package fill opacity.</summary>
    public TrackableValue<double> PackageOpacity => _store.DiagramOptionsEditor.PackageOpacity;

    /// <summary>Transitive dependency fill color.</summary>
    public TrackableValue<string> TransitiveFill => _store.DiagramOptionsEditor.TransitiveFill;

    /// <summary>Transitive dependency fill opacity.</summary>
    public TrackableValue<double> TransitiveOpacity => _store.DiagramOptionsEditor.TransitiveOpacity;

    /// <summary>Whether grouping containers are rendered.</summary>
    public TrackableValue<bool> GroupingEnabled => _store.DiagramOptionsEditor.GroupingEnabled;

    /// <summary>Group container background fill.</summary>
    public TrackableValue<string> GroupingFill => _store.DiagramOptionsEditor.GroupingFill;

    /// <summary>Group container background opacity.</summary>
    public TrackableValue<double> GroupingOpacity => _store.DiagramOptionsEditor.GroupingOpacity;

    /// <summary>The group name.</summary>
    public TrackableValue<string> GroupName => _store.DiagramOptionsEditor.GroupName;

    /// <summary>The group name alias.</summary>
    public TrackableValue<string> GroupNameAlias => _store.DiagramOptionsEditor.GroupNameAlias;

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>The format toggle items bound to the ItemsControl.</summary>
    public ReadOnlyObservableCollection<FormatToggleItem> FormatToggles { get; }

    private string? _stylesHexError;
    private string? _groupingHexError;

    /// <summary>
    /// The first hex validation error across Framework, Package, and Transitive fills,
    /// or <see langword="null"/> when all are valid.
    /// </summary>
    public string? StylesHexError
    {
        get => _stylesHexError;
        private set => this.RaiseAndSetIfChanged(ref _stylesHexError, value);
    }

    /// <summary>
    /// The hex validation error for the Grouping fill, or <see langword="null"/> when valid.
    /// </summary>
    public string? GroupingHexError
    {
        get => _groupingHexError;
        private set => this.RaiseAndSetIfChanged(ref _groupingHexError, value);
    }

    /// <summary>Initializes a new instance of <see cref="DiagramsViewModel"/>.</summary>
    /// <param name="store">The project document store providing the editing surface.</param>
    public DiagramsViewModel(IProjectDocumentStore store)
    {
        _store = store;

        FormatToggles = CreateFormatToggles();

        WireFormatToggleSync();
        WireHexError();
        WireValidation();
    }

    private static ReadOnlyObservableCollection<FormatToggleItem> CreateFormatToggles()
    {
        var toggles = Enum.GetValues<DiagramFormat>()
            .Select(format => new FormatToggleItem
            {
                Format = format,
                DisplayName = GetDisplayName(format),
                IconKind = GetIconKind(format)
            })
            .ToList();

        return new ReadOnlyObservableCollection<FormatToggleItem>(new ObservableCollection<FormatToggleItem>(toggles));
    }

    private void WireFormatToggleSync()
    {
        // When the Formats collection changes (e.g. document load), sync toggle states.
        Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                handler => Formats.Items.CollectionChanged += handler,
                handler => Formats.Items.CollectionChanged -= handler)
            .Subscribe(eventPattern => OnFormatsCollectionChanged(eventPattern.Sender, eventPattern.EventArgs))
            .DisposeWith(_disposables);

        // Initial sync: set toggle states from the current collection contents.
        // This covers the case where the ViewModel is created after a document is already loaded.
        SyncTogglesFromCollection();

        // When a toggle is clicked, add/remove from the Formats collection.
        foreach (var toggle in FormatToggles)
        {
            toggle
                .WhenAnyValue(toggleItem => toggleItem.IsChecked)
                .Subscribe(isChecked => OnToggleChanged(toggle, isChecked))
                .DisposeWith(_disposables);
        }
    }

    private void SyncTogglesFromCollection()
    {
        _isSyncing = true;

        foreach (var toggle in FormatToggles)
        {
            toggle.IsChecked = Formats.Items.Contains(toggle.Format);
        }

        _isSyncing = false;
    }

    private void OnFormatsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing)
        {
            return;
        }

        SyncTogglesFromCollection();
    }

    private void OnToggleChanged(FormatToggleItem toggle, bool isChecked)
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;

        if (isChecked && !Formats.Items.Contains(toggle.Format))
        {
            Formats.Items.Add(toggle.Format);
        }
        else if (!isChecked)
        {
            Formats.Items.Remove(toggle.Format);
        }

        _isSyncing = false;
    }

    private void WireHexError()
    {
        var hexPattern = HexPatternRegex();

        // When there's more than one error, report the first one.
        this.WhenAnyValue(
                vm => vm.FrameworkFill.Value,
                vm => vm.PackageFill.Value,
                vm => vm.TransitiveFill.Value,
                (framework, package, transitive) =>
                    GetHexError(framework, "Framework", hexPattern) ??
                    GetHexError(package, "Package", hexPattern) ??
                    GetHexError(transitive, "Transitive", hexPattern))
            .Subscribe(error => StylesHexError = error)
            .DisposeWith(_disposables);

        this.WhenAnyValue(
                vm => vm.GroupingFill.Value,
                grouping => GetHexError(grouping, "Grouping", hexPattern))
            .Subscribe(error => GroupingHexError = error)
            .DisposeWith(_disposables);
    }

    private void WireValidation()
    {
        this.ValidationRule(
            viewModel => viewModel.Formats.Items.Count,
            count => count > 0,
            "At least one diagram format must be selected.");

        var hexPattern = HexPatternRegex();

        this.ValidationRule(
            viewModel => viewModel.FrameworkFill.Value,
            fill => IsValidHex(fill, hexPattern),
            _ => "Framework fill must be a valid hex color (e.g. #FF0000).");

        this.ValidationRule(
            viewModel => viewModel.PackageFill.Value,
            fill => IsValidHex(fill, hexPattern),
            _ => "Package fill must be a valid hex color (e.g. #00FF00).");

        this.ValidationRule(
            viewModel => viewModel.TransitiveFill.Value,
            fill => IsValidHex(fill, hexPattern),
            _ => "Transitive fill must be a valid hex color (e.g. #0000FF).");

        this.ValidationRule(
            viewModel => viewModel.GroupingFill.Value,
            fill => IsValidHex(fill, hexPattern),
            _ => "Grouping fill must be a valid hex color (e.g. #E7EBFC).");

        // Display rules for BindValidation
        this.ValidationRule(
            viewModel => viewModel.StylesHexError,
            error => error is null,
            error => error ?? string.Empty);

        this.ValidationRule(
            viewModel => viewModel.GroupingHexError,
            error => error is null,
            error => error ?? string.Empty);
    }

    private static bool IsValidHex(string? fill, Regex hexPattern)
    {
        return fill.IsNullOrEmpty() || hexPattern.IsMatch(fill);
    }

    private static string? GetHexError(string? fill, string label, Regex hexPattern)
    {
        return fill.IsNotNullOrEmpty() && !hexPattern.IsMatch(fill)
            ? $"{label} fill must be a valid hex color (e.g. #FF0000)."
            : null;
    }

    private static string GetDisplayName(DiagramFormat format)
    {
        return format switch
        {
            DiagramFormat.D2 => "D2",
            DiagramFormat.Mermaid => "Mermaid",
            _ => format.ToString()
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    private static MaterialDesignThemes.Wpf.PackIconKind GetIconKind(DiagramFormat format)
    {
        return format switch
        {
            DiagramFormat.D2 => MaterialDesignThemes.Wpf.PackIconKind.VectorPolyline,
            DiagramFormat.Mermaid => MaterialDesignThemes.Wpf.PackIconKind.ChartSankeyVariant,
            _ => MaterialDesignThemes.Wpf.PackIconKind.GraphOutline
        };
    }
}
