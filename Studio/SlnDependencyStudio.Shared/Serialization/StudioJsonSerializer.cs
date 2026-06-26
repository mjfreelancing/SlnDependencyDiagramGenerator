using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlnDependencyStudio.Shared.Serialization;

/// <summary>Default implementation of <see cref="IStudioJsonSerializer"/> using System.Text.Json with
/// camelCase naming, indented output, and enum-as-string conversion.</summary>
internal sealed class StudioJsonSerializer : IStudioJsonSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc />
    public string Serialize<TValue>(TValue value)
    {
        return JsonSerializer.Serialize(value, SerializerOptions);
    }

    /// <inheritdoc />
    public Task SerializeAsync<TValue>(Stream stream, TValue? value, CancellationToken cancellationToken = default)
    {
        return JsonSerializer.SerializeAsync(stream, value, SerializerOptions, cancellationToken);
    }

    /// <inheritdoc />
    public TValue? Deserialize<TValue>([StringSyntax(StringSyntaxAttribute.Json)] string json)
    {
        return JsonSerializer.Deserialize<TValue>(json, SerializerOptions);
    }

    /// <inheritdoc />
    public ValueTask<TValue?> DeserializeAsync<TValue>(Stream stream, CancellationToken cancellationToken = default)
    {
        return JsonSerializer.DeserializeAsync<TValue>(stream, SerializerOptions, cancellationToken);
    }
}
