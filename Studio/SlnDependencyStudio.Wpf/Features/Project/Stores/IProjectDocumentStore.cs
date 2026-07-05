using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Wpf.Features.Project.Stores;

/// <summary>
/// The observable source of truth for the currently open dependency project document.
/// Holds the deserialized document snapshot, editor wrappers for each editable sub-object,
/// and derives global dirty state across all editing surfaces.
/// </summary>
public interface IProjectDocumentStore : IStudioSingletonDependency
{
    /// <summary>The file path from which the document was loaded.
    /// <see langword="null"/> for new or unsaved documents.</summary>
    /// <remarks>This property is Observable.</remarks>
    string? CurrentFilePath { get; }

    /// <summary>The editing wrapper for <see cref="DependencyProjectMetadata"/>.
    /// Contains <see cref="TrackableValue{T}"/> instances for each metadata field.</summary>
    IProjectMetadataEditor MetadataEditor { get; }

    /// <summary><see langword="true"/> when any editor wrapper has unsaved changes.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool IsDirty { get; }

    /// <summary><see langword="true"/> when a document is currently loaded.</summary>
    /// <remarks>This property is Observable.</remarks>
    bool HasDocument { get; }

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
    /// Updates <see cref="CurrentFilePath"/> and marks all editors as clean after a successful save.</summary>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task SaveAsAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Closes the current document, resets all editor wrappers, and clears the file path.</summary>
    void Close();
}
