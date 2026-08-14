using AllOverIt.Patterns.Enumeration;
using SlnDependencyStudio.Wpf.Converters.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SlnDependencyStudio.Wpf.Models;

/// <summary>Represents the application theme (light or dark).</summary>
[JsonConverter(typeof(StudioThemeJsonConverter))]
public sealed class StudioTheme : EnrichedEnum<StudioTheme>
{
    /// <summary>The light theme.</summary>
    public static readonly StudioTheme Light = new(1);

    /// <summary>The dark theme.</summary>
    public static readonly StudioTheme Dark = new(2);

    public StudioTheme(int value, [CallerMemberName] string name = "")
        : base(value, name)
    {
    }
}
