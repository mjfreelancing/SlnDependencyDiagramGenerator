using SlnDependencyStudio.Shared.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace SlnDependencyStudio.Shared.Serialization;

/// <summary>Provides JSON serialization and deserialization services with consistent options
/// (camelCase naming, indented output, enum-as-string conversion).</summary>
public interface IStudioJsonSerializer : IStudioScopedDependency
{
    /// <summary>Serializes the specified value to a JSON string.</summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>A JSON string representation.</returns>
    string Serialize<TValue>(TValue value);

    /// <summary>Serializes the specified value to a JSON stream asynchronously.</summary>
    /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
    /// <param name="stream">The stream to write the JSON to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the serialization has finished.</returns>
    Task SerializeAsync<TValue>(Stream stream, TValue? value, CancellationToken cancellationToken = default);

    /// <summary>Deserializes a JSON string to the specified type.</summary>
    /// <typeparam name="TValue">The type of the target object.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or <see langword="null"/> if the JSON represents a null literal.</returns>
    TValue? Deserialize<TValue>([StringSyntax(StringSyntaxAttribute.Json)] string json);

    /// <summary>Deserializes a JSON stream to the specified type asynchronously.</summary>
    /// <typeparam name="TValue">The type of the target object.</typeparam>
    /// <param name="stream">The stream containing the JSON to deserialize.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A value task that resolves to the deserialized object, or <see langword="null"/>.</returns>
    ValueTask<TValue?> DeserializeAsync<TValue>(Stream stream, CancellationToken cancellationToken = default);
}
