using AllOverIt.Patterns.Enumeration;
using SlnDependencyStudio.Wpf.Converters.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SlnDependencyStudio.Wpf.Models;

[JsonConverter(typeof(StudioThemeJsonConverter))]
public sealed class StudioTheme : EnrichedEnum<StudioTheme>
{
    public static readonly StudioTheme Light = new(1);

    public static readonly StudioTheme Dark = new(2);

    public StudioTheme(int value, [CallerMemberName] string name = "")
        : base(value, name)
    {
    }
}
