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
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Diagrams;

/// <summary>View model for the "Diagrams" navigation page. Delegates all document-related
/// bindings to the <see cref="IProjectDocumentStore"/> and manages format toggle state.</summary>
public sealed class DiagramsViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;

    // Guards against infinite recursion between toggle changes and collection sync.
    // Toggling a checkbox modifies the Formats collection, which fires CollectionChanged,
    // which syncs toggle states — without this flag, each sync would re-trigger the
    // toggle-change handler. The two-way sync is inherently circular; a guard flag is
    // the standard pattern when neither side can be made a pure projection of the other.
    private bool _isSyncing;

    /// <summary>The diagram formats collection. Items are added/removed as toggles change.</summary>
    public TrackableCollection<DiagramFormat> Formats => _store.DiagramOptionsEditor.Formats;

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>The format toggle items bound to the ItemsControl.</summary>
    public ReadOnlyObservableCollection<FormatToggleItem> FormatToggles { get; }

    /// <summary>Initializes a new instance of <see cref="DiagramsViewModel"/>.</summary>
    /// <param name="store">The project document store providing the editing surface.</param>
    public DiagramsViewModel(IProjectDocumentStore store)
    {
        _store = store;

        FormatToggles = CreateFormatToggles();

        WireFormatToggleSync();
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
        Formats.Items.CollectionChanged += OnFormatsCollectionChanged;

        // Initial sync: set toggle states from the current collection contents.
        // This covers the case where the ViewModel is created after a document is already loaded.
        SyncTogglesFromCollection();

        // When a toggle is clicked, add/remove from the Formats collection.
        foreach (var toggle in FormatToggles)
        {
            toggle
                .WhenAnyValue(toggleItem => toggleItem.IsChecked)
                .Subscribe(isChecked => OnToggleChanged(toggle, isChecked));
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

    private void WireValidation()
    {
        this.ValidationRule(
            viewModel => viewModel.Formats.Items.Count,
            count => count > 0,
            "At least one diagram format must be selected.");
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
