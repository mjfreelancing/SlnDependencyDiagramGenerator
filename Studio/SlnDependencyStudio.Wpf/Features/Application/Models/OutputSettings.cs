namespace SlnDependencyStudio.Wpf.Features.Application.Models;

/// <summary>Output panel preferences persisted across sessions.</summary>
public sealed class OutputSettings
{
    /// <summary>Whether text in the output panel wraps to the next line. Defaults to <see langword="false"/>.</summary>
    public bool WrapContent { get; set; }

    /// <summary>Whether verbose logging (all application log events) is enabled. Defaults to <see langword="true"/>.</summary>
    public bool IsVerboseLogging { get; set; } = true;
}
