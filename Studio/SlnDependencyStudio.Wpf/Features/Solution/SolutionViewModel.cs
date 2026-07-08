using AllOverIt.Extensions;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Solution;

/// <summary>View model for the "Solution" navigation page. Delegates all document-related
/// bindings to the <see cref="IProjectDocumentStore"/> so that the store is the single
/// source of truth for the currently open document.</summary>
public sealed class SolutionViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;

    /// <summary>The solution path. Bound via <c>SolutionPath.Value</c> in XAML.</summary>
    public TrackableValue<string> SolutionPath => _store.SolutionOptionsEditor.SolutionPath;

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>Interaction that asks the view to browse for a .sln/.slnx file.</summary>
    public Interaction<string, string?> BrowseSolutionPathInteraction { get; } = new();

    /// <summary>Command that opens a file browser for the solution path.</summary>
    public ReactiveCommand<Unit, Unit> BrowseSolutionPathCommand { get; }

    /// <summary>Initializes a new instance of <see cref="SolutionViewModel"/>.</summary>
    /// <param name="store">The project document store providing the editing surface.</param>
    public SolutionViewModel(IProjectDocumentStore store)
    {
        _store = store;

        WireValidation();
        BrowseSolutionPathCommand = CreateBrowseCommand();
    }

    private void WireValidation()
    {
        this.ValidationRule(
            viewModel => viewModel.SolutionPath.Value,
            path => path.IsNotNullOrEmpty(),
            "Solution path must not be empty.");

        this.ValidationRule(
            viewModel => viewModel.SolutionPath.Value,
            path =>
            {
                if (path.IsNullOrEmpty())
                {
                    return true;
                }

                var resolvedPath = PathUtils.ResolveAsAbsolutePath(path, _store.DocumentDirectory);

                return File.Exists(resolvedPath);
            },
            "Solution file not found at the specified path.");
    }

    private ReactiveCommand<Unit, Unit> CreateBrowseCommand()
    {
        return ReactiveCommand.CreateFromObservable(() =>
        {
            var resolvedPath = PathUtils.ResolveAsAbsolutePath(SolutionPath.Value ?? string.Empty, _store.DocumentDirectory);

            return BrowseSolutionPathInteraction
                .Handle(resolvedPath)
                .Do(result =>
                {
                    if (result is not null)
                    {
                        SolutionPath.Value = PathUtils.MakeRelativeIfPossible(result, _store.DocumentDirectory);
                    }
                })
                .Select(_ => Unit.Default);
        });
    }
}
