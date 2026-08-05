using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.PreGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;
using SlnDependencyStudio.Wpf.Features.Solution;
using System.ComponentModel;

namespace SlnDependencyStudio.Wpf.Features.Project.Stores;

/// <summary>
/// The observable source of truth for the currently open dependency project document.
/// Holds the deserialized document snapshot, editor wrappers for each editable sub-object,
/// and derives global dirty state across all editing surfaces.
/// </summary>
/// <remarks>
/// Declares <see cref="INotifyPropertyChanged"/> so that the members documented as
/// "Observable" form an enforceable contract rather than a documentation convention,
/// and so observers (such as the shell's navigation pipeline) can rely on change
/// notifications regardless of the concrete store implementation.
/// </remarks>
public interface IProjectDocumentStore : IStudioSingletonDependency, INotifyPropertyChanged
{
    /// <summary>The file path from which the document was loaded.
    /// <see langword="null"/> for new or unsaved documents.</summary>
    /// <remarks>This property is Observable.</remarks>
    string? DocumentFilePath { get; }

    /// <summary>The directory containing the current document.
    /// Returns <see cref="string.Empty"/> when <see cref="DocumentFilePath"/> is <see langword="null"/>.</summary>
    string DocumentDirectory { get; }

    /// <summary>The editing wrapper for <see cref="DependencyProjectMetadata"/>.
    /// Contains <see cref="TrackableValue{T}"/> instances for each metadata field.</summary>
    IProjectMetadataEditor MetadataEditor { get; }

    /// <summary>The editing wrapper for <see cref="GeneratorSolutionOptions"/>.
    /// Contains <see cref="TrackableValue{T}"/> instances for solution options.</summary>
    ISolutionOptionsEditor SolutionOptionsEditor { get; }

    /// <summary>The editing wrapper for <see cref="GeneratorExportOptions"/>.
    /// Contains <see cref="TrackableValue{T}"/> instances for export options.</summary>
    IExportOptionsEditor ExportOptionsEditor { get; }

    /// <summary>The editing wrapper for <see cref="GeneratorDiagramOptions"/>.
    /// Contains <see cref="TrackableValue{T}"/> instances for diagram options.</summary>
    IDiagramOptionsEditor DiagramOptionsEditor { get; }

    /// <summary>The editing wrapper for <see cref="PreGenerationConfig"/>.
    /// Contains <see cref="TrackableValue{T}"/> instances for pre-generation options.</summary>
    IPreGenerationConfigEditor PreGenerationEditor { get; }

    /// <summary>The editing wrapper for the <c>RestoreSolution</c> flag.
    /// Contains a <see cref="TrackableValue{T}"/> for the restore solution option.</summary>
    IRestoreSolutionEditor RestoreSolutionEditor { get; }

    /// <summary>The editing wrapper for <see cref="PostGenerationConfig"/>.
    /// Contains <see cref="TrackableValue{T}"/> instances for post-generation options.</summary>
    IPostGenerationConfigEditor PostGenerationEditor { get; }

    /// <summary><see langword="true"/> when any editor wrapper has unsaved changes.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary><see langword="true"/> when a document is currently loaded.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool HasDocument { get; }

    /// <summary><see langword="true"/> while the document is opening or closing
    /// and editor wrappers are being populated or reset.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsTransitioning { get; }

    // This property is the canonical signal for "a document was loaded". The shell keys
    // its single navigation pipeline off it so that page selection can be derived from
    // document state rather than commanded imperatively from each operation that loads a
    // document.
    //
    // Neither HasDocument nor DocumentFilePath can serve this role on their own:
    //
    // - HasDocument only changes when the first document is loaded (false → true) and when
    //   it is closed (true → false). Opening a second document while one is already open
    //   leaves it true, so no change notification is raised and a subscriber keyed on it
    //   would never fire — yet the UI still needs to land on the Project page in that case.
    //
    // - DocumentFilePath also changes on SaveAsAsync — but saving under a new name must not
    //   trigger navigation (the user stays on the page they are editing). Subscribing to the
    //   path would therefore fire navigation on an action that must leave the current page
    //   untouched.
    //
    // DocumentEpoch changes on exactly the right occasions: every successful open (first or
    // subsequent), and never on save or save-as. The shell's single navigation pipeline keys
    // off this counter and uses HasDocument only to decide the direction of the resulting
    // transition — showing the Project page when a document is loaded, or the empty state
    // when it is not.
    //
    /// <summary>
    /// A monotonically increasing counter that is incremented each time a document is
    /// successfully opened via <see cref="OpenAsync"/>, including when one open document
    /// is replaced by another, and reset to <c>0</c> when the document is closed.
    /// </summary>
    /// <remarks>
    /// This property is Observable.
    /// </remarks>
    int DocumentEpoch { get; }

    /// <summary>Opens and deserializes a dependency project from the specified file path,
    /// populating all editor wrappers from the document.</summary>
    /// <param name="filePath">The path to the <c>.sds</c> file.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task OpenAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Flushes all editor wrapper values to the document and serializes to the current file path.
    /// Marks all editors as clean after a successful save.</summary>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>Flushes all editor wrapper values to the document and serializes to the specified file path.
    /// Updates <see cref="DocumentFilePath"/> and marks all editors as clean after a successful save.</summary>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Closes the current document, resets all editor wrappers, and clears the file path.</summary>
    void Close();

    /// <summary>Flushes all editor wrappers to the underlying document and returns a
    /// <see cref="DependencyGeneratorConfig"/> snapshot ready for generation.</summary>
    DependencyGeneratorConfig BuildGeneratorConfig();

    /// <summary>Flushes all editor wrappers to the underlying document and returns the full document
    /// with relative solution/export paths resolved to absolute paths (mirroring <see cref="BuildGeneratorConfig"/>).</summary>
    DependencyProjectDocument BuildDocument();
}
