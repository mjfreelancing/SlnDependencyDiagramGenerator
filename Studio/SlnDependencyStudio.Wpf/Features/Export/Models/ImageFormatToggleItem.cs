using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyStudio.Wpf.Features.Export.Models;

/// <summary>View model for a single image format toggle in the Export page.</summary>
public sealed class ImageFormatToggleItem : ReactiveObject
{
    private bool _isChecked;

    /// <summary>The <see cref="DiagramImageFormat"/> enum value this toggle represents.</summary>
    public DiagramImageFormat Format { get; init; }

    /// <summary>Human-readable display name shown next to the toggle.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Material Design icon kind for this format.</summary>
    public MaterialDesignThemes.Wpf.PackIconKind IconKind { get; init; }

    /// <summary>Whether this format is currently selected.</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}
