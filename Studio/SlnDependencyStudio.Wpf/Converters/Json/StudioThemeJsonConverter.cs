using SlnDependencyStudio.Wpf.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlnDependencyStudio.Wpf.Converters.Json;

/// <summary>JSON converter for <see cref="StudioTheme"/> that serializes the <see cref="EnrichedEnum{StudioTheme}.Name"/>
/// value (e.g. "Light", "Dark") as a string and deserializes back to the matching singleton instance.</summary>
public sealed class StudioThemeJsonConverter : JsonConverter<StudioTheme>
{
    public override StudioTheme? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var name = reader.GetString();

        return name is null
            ? null
            : StudioTheme.From(name);
    }

    public override void Write(Utf8JsonWriter writer, StudioTheme value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Name);
    }
}
