using SlnDependencyStudio.Shared.Config;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlnDependencyStudio.Shared.Serialization;

/// <summary>Handles JSON serialization and deserialization of <see cref="DependencyProjectDocument"/>,
/// including schema versioning, forward-compatible unknown field handling, and migration between schema versions.</summary>
internal sealed class DependencyProjectSerializer : IDependencyProjectSerializer
{
    /// <summary>The current schema version of the document format.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Schema migration steps, keyed by source version. Each step transforms the document
    /// from that version to the next. Migrations are applied in ascending version order until the
    /// document reaches <see cref="CurrentSchemaVersion"/>.</summary>
    private static readonly SortedDictionary<int, Action<DependencyProjectDocument>> Migrations = new()
    {
        // Example for future use:
        // { 1, document => { document.SchemaVersion = 2; /* transform fields */ } },
    };

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Serializes a document to a JSON string.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <returns>A JSON string representation.</returns>
    public string Serialize(DependencyProjectDocument document)
    {
        return JsonSerializer.Serialize(document, Options);
    }

    /// <summary>Serializes a document to a JSON file.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="filePath">The target file path.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    public async Task SerializeAsync(DependencyProjectDocument document, string filePath)
    {
        var json = Serialize(document);
        await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
    }

    /// <summary>Deserializes a JSON string into a document.</summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized document.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the schema version is not supported.</exception>
    public DependencyProjectDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<DependencyProjectDocument>(json, Options)
            ?? throw new InvalidOperationException("Failed to deserialize the dependency project document.");

        if (document.SchemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"The document schema version {document.SchemaVersion} is not supported. " +
                $"The latest supported version is {CurrentSchemaVersion}.");
        }

        MigrateToCurrent(document);

        return document;
    }

    /// <summary>Loads and deserializes a document from a JSON file.</summary>
    /// <param name="configFilename">The configuration file path.</param>
    /// <returns>A task that resolves to the deserialized document.</returns>
    public async Task<DependencyProjectDocument> DeserializeAsync(string configFilename, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(configFilename, cancellationToken).ConfigureAwait(false);
        return Deserialize(json);
    }

    private static void MigrateToCurrent(DependencyProjectDocument document)
    {
        var orderedMigrations = Migrations
            .Where(kvp => kvp.Key >= document.SchemaVersion)
            .OrderBy(kvp => kvp.Key);

        foreach (var (_, migration) in orderedMigrations)
        {
            migration(document);
        }
    }
}