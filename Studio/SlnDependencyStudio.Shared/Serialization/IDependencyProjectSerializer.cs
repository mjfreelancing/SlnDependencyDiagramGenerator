using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.DependencyInjection;

namespace SlnDependencyStudio.Shared.Serialization;

/// <summary>Handles JSON serialization and deserialization of <see cref="DependencyProjectDocument"/>.</summary>
public interface IDependencyProjectSerializer : IStudioScopedDependency
{
    /// <summary>Serializes a document to a JSON string.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <returns>A JSON string representation.</returns>
    string Serialize(DependencyProjectDocument document);

    /// <summary>Serializes a document to a JSON file.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="filePath">The target file path.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    Task SerializeAsync(DependencyProjectDocument document, string filePath, CancellationToken cancellationToken = default);

    /// <summary>Deserializes a JSON string into a document.</summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized document.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the document's schema version is not supported.</exception>
    DependencyProjectDocument Deserialize(string json);

    /// <summary>Loads and deserializes a document from a JSON file.</summary>
    /// <param name="projectFilename">The path to the project file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that resolves to the deserialized document.</returns>
    Task<DependencyProjectDocument> DeserializeAsync(string projectFilename, CancellationToken cancellationToken = default);
}
