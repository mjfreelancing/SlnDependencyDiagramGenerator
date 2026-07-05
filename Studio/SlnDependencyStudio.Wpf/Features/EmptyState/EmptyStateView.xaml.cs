using ReactiveUI;

namespace SlnDependencyStudio.Wpf.Features.EmptyState;

/// <summary>Code-behind for the empty-state landing page view.</summary>
public partial class EmptyStateView : ReactiveUserControl<EmptyStateViewModel>
{
    public EmptyStateView(EmptyStateViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        InitializeComponent();
    }
}
