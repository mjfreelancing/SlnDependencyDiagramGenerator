using AllOverIt.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlnDependencyDiagramGenerator.Generator;

/// <summary>Generates and caches markdown badges for target frameworks.</summary>
internal sealed class TargetFrameworkBadgeProvider
{
    /// <summary>The Shields.io badge endpoint prefix used to build markdown badge URLs.</summary>
    private const string BadgeHost = "https://img.shields.io/badge/.NET";

    /// <summary>The palette used for round-robin base-moniker color assignment.</summary>
    private static readonly string[] Palette =
    [
        ColorCode.Blue,
        ColorCode.Green,
        ColorCode.Purple,
        ColorCode.Orange,
        ColorCode.Red,
        ColorCode.Yellow
    ];

    /// <summary>Cache of markdown badges by full target framework moniker.</summary>
    private readonly Dictionary<string, string> _badgeCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Cache of assigned colors by base moniker (for example, net8.0).</summary>
    private readonly Dictionary<string, string> _baseMonikerColors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The next palette index to use when assigning a color to a newly-seen base moniker.</summary>
    private int _nextColorIndex;

    /// <summary>Returns a markdown badge for the specified target framework moniker.</summary>
    /// <param name="targetFramework">The target framework moniker.</param>
    /// <returns>The markdown badge text.</returns>
    public string GetBadge(string targetFramework)
    {
        if (_badgeCache.TryGetValue(targetFramework, out var cachedBadge))
        {
            return cachedBadge;
        }

        var normalizedFramework = NormalizeFramework(targetFramework);
        var baseMoniker = GetBaseMoniker(normalizedFramework);
        var color = GetColor(baseMoniker);
        var message = BuildBadgeMessage(normalizedFramework);
        var badge = $"![]({BadgeHost}-{message}-{color}.svg)";

        _badgeCache[targetFramework] = badge;

        return badge;
    }

    /// <summary>Gets the color for a base moniker, assigning one on first use via round-robin.</summary>
    /// <param name="baseMoniker">The base target framework moniker.</param>
    /// <returns>The hex color code assigned to the base moniker.</returns>
    private string GetColor(string baseMoniker)
    {
        if (_baseMonikerColors.TryGetValue(baseMoniker, out var color))
        {
            return color;
        }

        color = Palette[_nextColorIndex];
        _nextColorIndex = (_nextColorIndex + 1) % Palette.Length;

        _baseMonikerColors[baseMoniker] = color;

        return color;
    }

    /// <summary>Normalizes a target framework into base moniker plus optional profile family.</summary>
    /// <param name="framework">The target framework moniker to normalize.</param>
    /// <returns>A normalized framework in the form base or base-profile.</returns>
    private static string NormalizeFramework(string framework)
    {
        var baseMoniker = GetBaseMoniker(framework);
        var profile = GetProfileFamily(framework);

        return profile.IsNullOrEmpty()
            ? baseMoniker
            : $"{baseMoniker}-{profile}";
    }

            /// <summary>Builds the Shields.io message segment from a normalized framework moniker.</summary>
            /// <param name="normalizedFramework">The normalized framework moniker.</param>
            /// <returns>The message portion used in the badge URL.</returns>
    private static string BuildBadgeMessage(string normalizedFramework)
    {
        var baseMoniker = GetBaseMoniker(normalizedFramework);
        var profile = GetProfileFamily(normalizedFramework);

        var message = baseMoniker.StartsWith("net", StringComparison.OrdinalIgnoreCase)
            ? baseMoniker[3..]
            : baseMoniker;

        if (profile.IsNullOrEmpty())
        {
            return message;
        }

        return $"{message}--{profile}";
    }

    /// <summary>Gets the base moniker component (text before the first hyphen).</summary>
    /// <param name="framework">The target framework moniker.</param>
    /// <returns>The base moniker.</returns>
    private static string GetBaseMoniker(string framework)
    {
        var separator = framework.IndexOf('-');

        return separator < 0
            ? framework
            : framework[..separator];
    }

            /// <summary>Extracts the profile family from the suffix of a framework moniker.</summary>
            /// <param name="framework">The target framework moniker.</param>
            /// <returns>The lower-cased leading alphabetic profile family, or an empty string when absent.</returns>
    private static string GetProfileFamily(string framework)
    {
        var separator = framework.IndexOf('-');

        if (separator < 0 || separator == framework.Length - 1)
        {
            return string.Empty;
        }

        var suffix = framework[(separator + 1)..];
        var builder = new StringBuilder();

        foreach (var character in suffix)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
