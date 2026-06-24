namespace SlnDependencyStudio.Wpf.Features.Application.Models;

/// <summary>Serializable window position, size, and state.</summary>
public sealed class WindowPlacement
{
    /// <summary>The left edge of the window in screen coordinates.</summary>
    public double Left { get; set; }

    /// <summary>The top edge of the window in screen coordinates.</summary>
    public double Top { get; set; }

    /// <summary>The width of the window.</summary>
    public double Width { get; set; }

    /// <summary>The height of the window.</summary>
    public double Height { get; set; }

    /// <summary>The window state: Normal, Maximized, or Minimized.</summary>
    public string State { get; set; } = "Normal";
}
