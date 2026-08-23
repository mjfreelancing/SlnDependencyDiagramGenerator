using System.Reflection;

namespace SlnDependencyStudio.Wpf.Utils;

/// <summary>Provides the application display version, read from the assembly metadata.</summary>
internal static class ApplicationVersion
{
    /// <summary>Gets the application display version (for example "1.0.0-rc1") from the assembly's informational version.</summary>
    public static string Value { get; } = ResolveVersion();

    private static string ResolveVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (informationalVersion is not null)
        {
            // The SDK appends the SourceRevisionId (a git hash or a generated GUID outside CI) to the
            // informational version as a "+suffix". Strip it so the displayed version stays clean, e.g.
            // "1.0.0-rc1+<guid>" -> "1.0.0-rc1".
            var sourceRevisionIndex = informationalVersion.IndexOf('+');

            if (sourceRevisionIndex >= 0)
            {
                informationalVersion = informationalVersion[..sourceRevisionIndex];
            }
        }

        return informationalVersion ?? assembly.GetName().Version!.ToString(3);
    }
}
