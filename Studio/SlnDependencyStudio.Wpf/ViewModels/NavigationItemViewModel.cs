using AllOverIt.ReactiveUI.Factories;
using MaterialDesignThemes.Wpf;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SlnDependencyStudio.Wpf.ViewModels;

/// <summary>Abstract base class for navigation items displayed in the left sidebar.</summary>
public abstract class NavigationItemViewModel : ReactiveObject
{
    /// <summary>The display label shown in the nav (e.g. "Project", "Sources").</summary>
    [Reactive]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>The Material Design icon kind for this nav item's icon.</summary>
    [Reactive]
    public PackIconKind IconKind { get; set; }

    /// <summary>Whether the page associated with this nav item has validation errors.
    /// Controls the warning dot indicator in the nav.</summary>
    [Reactive]
    public bool HasValidationError { get; set; }

    /// <summary>When <see langword="true"/>, this nav item is rendered with a Material Design
    /// <c>Chip</c> showing "optional" text (e.g. the Pipeline item).</summary>
    [Reactive]
    public bool IsAdvanced { get; set; }

    /// <summary>The CLR <see cref="Type"/> of the page view model associated with this nav item.
    /// Used to resolve the correct view via <see cref="IViewFactory"/>.</summary>
    public abstract Type ViewModelType { get; }

    /// <summary>Creates the view for this nav item's page, invoking <see cref="ConfigureViewModel"/>
    /// if set.</summary>
    /// <param name="viewFactory">The view factory used to create view/view-model pairs.</param>
    /// <returns>The created view, with its <c>ViewModel</c> populated.</returns>
    public abstract IViewFor CreateView(IViewFactory viewFactory);
}

/// <summary>Generic navigation item for a specific page view model type.
/// <typeparamref name="TViewModel"/> must be a reference type registered with
/// <see cref="IViewFactory"/>.</summary>
/// <typeparam name="TViewModel">The page view model type.</typeparam>
public sealed class NavigationItemViewModel<TViewModel> : NavigationItemViewModel where TViewModel : class
{
    /// <inheritdoc />
    public override Type ViewModelType => typeof(TViewModel);

    /// <inheritdoc />
    public override IViewFor CreateView(IViewFactory viewFactory)
    {
        return viewFactory.CreateViewFor<TViewModel>();
    }
}
