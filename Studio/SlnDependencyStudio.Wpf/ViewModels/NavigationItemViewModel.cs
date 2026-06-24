using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SlnDependencyStudio.Wpf.ViewModels;

/// <summary>View model for a single navigation item in the left sidebar.</summary>
public sealed class NavigationItemViewModel : ReactiveObject
{
    /// <summary>The display label shown in the nav (e.g. "Project", "Sources").</summary>
    [Reactive]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The Material Design icon kind for this nav item's icon.</summary>
    [Reactive]
    public PackIconKind IconKind { get; set; }

    /// <summary>Whether this nav item is the currently selected one.</summary>
    [Reactive]
    public bool IsSelected { get; set; }

    /// <summary>Whether the page associated with this nav item has validation errors.
    /// Controls the warning dot indicator in the nav.</summary>
    [Reactive]
    public bool HasValidationError { get; set; }

    /// <summary>The <see cref="Type"/> of the view model that should be loaded in the centre workspace
    /// when this nav item is selected. Used for view resolution.</summary>
    [Reactive]
    public Type? ViewModelType { get; set; }

    /// <summary>When <see langword="true"/>, this nav item is rendered with a Material Design
    /// <c>Chip</c> showing "optional" text (e.g. the Pipeline item).</summary>
    [Reactive]
    public bool IsAdvanced { get; set; }
}
