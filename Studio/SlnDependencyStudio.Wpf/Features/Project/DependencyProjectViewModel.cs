using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using SlnDependencyStudio.Shared.Config;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>Document-level view model wrapping a <see cref="DependencyProjectDocument"/>.
/// Holds the current file path, tracks dirty state across all page view models, and
/// will be extended with child view models for each navigation section (Phase 4/5).</summary>
public sealed class DependencyProjectViewModel : ReactiveObject
{
    /// <summary>The underlying dependency project document.</summary>
    public DependencyProjectDocument Document { get; }

    /// <summary>The file path from which the document was loaded.
    /// <see langword="null"/> for new documents that have not yet been saved.</summary>
    public string? CurrentFilePath { get; set; }

    /// <summary><see langword="true"/> when the Project page has unsaved changes.
    /// Fed by page VMs via <c>BindTo</c>.</summary>
    [Reactive]
    public bool IsProjectPageDirty { get; set; }

    /// <summary><see langword="true"/> when any page has unsaved changes.
    /// Composed from all per-page dirty flags.</summary>
    public bool IsDirty => IsProjectPageDirty /* || IsSourcesPageDirty in future */;

    /// <summary>Initializes a new instance of <see cref="DependencyProjectViewModel"/>.</summary>
    /// <param name="document">The underlying document to wrap. Must not be <see langword="null"/>.</param>
    /// <param name="filePath">Optional file path the document was loaded from.</param>
    public DependencyProjectViewModel(DependencyProjectDocument document, string? filePath = null)
    {
        Document = document;
        CurrentFilePath = filePath;

        // Re-raise IsDirty when any page-level dirty flag changes.
        this.WhenAnyValue(vm => vm.IsProjectPageDirty)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(IsDirty)));
    }
}
