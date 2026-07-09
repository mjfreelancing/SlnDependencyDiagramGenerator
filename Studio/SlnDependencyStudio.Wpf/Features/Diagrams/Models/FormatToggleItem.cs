using ReactiveUI;
using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyStudio.Wpf.Features.Diagrams.Models;

/// <summary>View model for a single diagram format toggle in the Diagrams page.
/// Each instance represents one <see cref="DiagramFormat"/> value with its display
/// name, icon, and checked state.</summary>
public sealed class FormatToggleItem : ReactiveObject
{
    private bool _isChecked;

    /// <summary>The <see cref="DiagramFormat"/> enum value this toggle represents.</summary>
    public DiagramFormat Format { get; init; }

    /// <summary>Human-readable display name shown next to the toggle.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>Material Design icon kind for this format.</summary>
    public MaterialDesignThemes.Wpf.PackIconKind IconKind { get; init; }

    /// <summary>Whether this format is currently selected. Changing this property
    /// triggers the parent ViewModel to add/remove from the <c>Formats</c> collection.</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}
