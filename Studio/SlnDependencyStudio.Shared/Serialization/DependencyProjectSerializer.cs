using Microsoft.Extensions.Logging;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Exceptions;

namespace SlnDependencyStudio.Shared.Serialization;

/// <summary>Handles JSON serialization and deserialization of <see cref="DependencyProjectDocument"/>,
/// including schema versioning, forward-compatible unknown field handling, and migration between schema versions.</summary>
internal sealed class DependencyProjectSerializer : IDependencyProjectSerializer
{
    /// <summary>A single schema migration step: transforms a document from its source version (the
    /// <see cref="Migrations"/> key) to <see cref="ToVersion"/>.</summary>
    /// <param name="ToVersion">The schema version the document must have after the step is applied.</param>
    /// <param name="Apply">The transform that performs the migration.</param>
    internal sealed record MigrationStep(int ToVersion, Action<DependencyProjectDocument> Apply);

    /// <summary>
    /// Schema migrations, keyed by the source schema version each step migrates FROM. Each step
    /// records its target version explicitly so <see cref="MigrateToCurrent"/> can walk the chain
    /// and verify every step advances the document as declared.
    /// </summary>
    private static readonly Dictionary<int, MigrationStep> Migrations = new()
    {
        // Example for future use:
        // { 1, new MigrationStep(ToVersion: 2, Apply: document => { /* transform fields */ }) },
    };

    private readonly IStudioJsonSerializer _jsonSerializer;
    private readonly ILogger<DependencyProjectSerializer> _logger;

    /// <summary>The current schema version of the document format.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>Initializes a new instance of <see cref="DependencyProjectSerializer"/>.</summary>
    /// <param name="jsonSerializer">The JSON serializer to delegate serialization and deserialization to.</param>
    /// <param name="logger">The logger instance.</param>
    public DependencyProjectSerializer(IStudioJsonSerializer jsonSerializer, ILogger<DependencyProjectSerializer> logger)
    {
        _jsonSerializer = jsonSerializer;
        _logger = logger;
    }

    /// <summary>Serializes a document to a JSON string.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <returns>A JSON string representation.</returns>
    public string Serialize(DependencyProjectDocument document)
    {
        return _jsonSerializer.Serialize(document);
    }

    /// <summary>Serializes a document to a JSON file.</summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="filePath">The target file path.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    public async Task SerializeAsync(DependencyProjectDocument document, string filePath, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Serializing project to {FilePath}", filePath);

        try
        {
            var json = Serialize(document);
            await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
        }
        // Cancellation is a normal shutdown path — do not log it as a failure.
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError("Failed to serialize project to {FilePath}: {ErrorMessage}", filePath, exception.Message);
            throw;
        }
    }

    /// <summary>Deserializes a JSON string into a document.</summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized document.</returns>
    /// <exception cref="DependencyProjectException">Thrown when the JSON is empty or not a valid dependency project document.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the schema version is not supported.</exception>
    public DependencyProjectDocument Deserialize(string json)
    {
        // System.Text.Json returns null for the JSON literal "null". Guard so an empty/invalid
        // document surfaces as a clear error.
        var document = _jsonSerializer.Deserialize<DependencyProjectDocument>(json)
            ?? throw new DependencyProjectException("The document is empty or not a valid dependency project document.");

        if (document.SchemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"The document schema version {document.SchemaVersion} is not supported by this version of " +
                $"SlnDependencyStudio (latest supported: {CurrentSchemaVersion}). Update the application to open this file.");
        }

        MigrateToCurrent(document);

        return document;
    }

    /// <summary>Loads and deserializes a document from a JSON file.</summary>
    /// <param name="configFilename">The configuration file path.</param>
    /// <returns>A task that resolves to the deserialized document.</returns>
    public async Task<DependencyProjectDocument> DeserializeAsync(string configFilename, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deserializing project from {ConfigFilename}", configFilename);

        try
        {
            var json = await File.ReadAllTextAsync(configFilename, cancellationToken).ConfigureAwait(false);
            return Deserialize(json);
        }
        // Cancellation is a normal shutdown path — do not log it as a failure.
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError("Failed to deserialize project from {ConfigFilename}: {ErrorMessage}", configFilename, exception.Message);
            throw;
        }
    }

    private static void MigrateToCurrent(DependencyProjectDocument document)
    {
        MigrateToCurrent(document, Migrations);
    }

    /// <summary>
    /// Walks the explicit (from → to) migration chain using the supplied table: applies the step for
    /// the document's current version, asserts it advanced to the declared target, and repeats until
    /// the document reaches <see cref="CurrentSchemaVersion"/>. Internal so tests can exercise the
    /// chain with their own migration table without mutating the production map.
    /// </summary>
    /// <param name="document">The document to migrate.</param>
    /// <param name="migrations">The migration table, keyed by source schema version.</param>
    /// <exception cref="InvalidOperationException">Thrown when a migration step is missing, or a step does not advance the version as declared.</exception>
    internal static void MigrateToCurrent(DependencyProjectDocument document, IReadOnlyDictionary<int, MigrationStep> migrations)
    {
        // Walk the explicit (from -> to) migration chain: apply the step for the document's current
        // version, assert it advanced to the declared target, then repeat until the document reaches
        // CurrentSchemaVersion. This fails fast instead of silently leaving a stale version when a
        // step is missing or a migration does not advance the version it declares.
        while (document.SchemaVersion < CurrentSchemaVersion)
        {
            var sourceVersion = document.SchemaVersion;

            if (!migrations.TryGetValue(sourceVersion, out var migration))
            {
                throw new InvalidOperationException(
                    $"No migration is defined from schema version {sourceVersion} to {CurrentSchemaVersion}. A migration step is missing.");
            }

            migration.Apply(document);

            if (document.SchemaVersion != migration.ToVersion)
            {
                throw new InvalidOperationException(
                    $"The migration from schema version {sourceVersion} did not advance the document to the declared version {migration.ToVersion}.");
            }
        }
    }
}
