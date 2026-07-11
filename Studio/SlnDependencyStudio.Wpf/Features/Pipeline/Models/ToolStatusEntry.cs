using ReactiveUI;
using System;

namespace SlnDependencyStudio.Wpf.Features.Pipeline.Models;

/// <summary>
/// Observable view-model entry representing the detection status of a single
/// external CLI tool (d2 or mmdc).
/// </summary>
public sealed class ToolStatusEntry : ReactiveObject
{
    private string _toolName = string.Empty;
    private bool _isAvailable;
    private string? _resolvedPath;
    private DateTime _lastChecked;
    private string? _errorMessage;
    private string _statusText = string.Empty;

    /// <summary>The tool name (e.g. "d2", "mmdc").</summary>
    public string ToolName
    {
        get => _toolName;
        set => this.RaiseAndSetIfChanged(ref _toolName, value);
    }

    /// <summary>Whether the tool was found and is usable.</summary>
    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            this.RaiseAndSetIfChanged(ref _isAvailable, value);
            RefreshStatusText();
        }
    }

    /// <summary>The resolved executable path, or <see langword="null"/> if not found.</summary>
    public string? ResolvedPath
    {
        get => _resolvedPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _resolvedPath, value);
            RefreshStatusText();
        }
    }

    /// <summary>When the last scan was performed.</summary>
    public DateTime LastChecked
    {
        get => _lastChecked;
        set => this.RaiseAndSetIfChanged(ref _lastChecked, value);
    }

    /// <summary>An error message when the tool is not available, or <see langword="null"/>.</summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
    }

    /// <summary>
    /// Human-readable status text: "Found at {path}" when available,
    /// otherwise "Not found — install the tool or set a path override".
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => this.RaiseAndSetIfChanged(ref _statusText, value);
    }

    private void RefreshStatusText()
    {
        StatusText = _isAvailable && _resolvedPath is not null
            ? $"Found at {_resolvedPath}"
            : "Not found — install the tool or set a path override";
    }
}
