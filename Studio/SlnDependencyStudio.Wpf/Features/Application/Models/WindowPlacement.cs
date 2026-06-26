using System.Windows;

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

    /// <summary>Determines whether the window rect described by this placement intersects any
    /// available screen area. Returns <see langword="false"/> when the saved position would
    /// land the window entirely off-screen (e.g. after a monitor is disconnected).</summary>
    public bool IsOnScreen()
    {
        var virtualRect = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        var windowRect = new Rect(Left, Top, Width, Height);

        return windowRect.IntersectsWith(virtualRect);
    }
}
