using AllOverIt.Extensions;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Components;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace SlnDependencyStudio.Wpf.Features.Solution;

/// <summary>View model for the "Solution" navigation page. Delegates all document-related
/// bindings to the <see cref="IProjectDocumentStore"/> so that the store is the single
/// source of truth for the currently open document.</summary>
public sealed class SolutionViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;

    /// <summary>The solution path. Bound via <c>SolutionPath.Value</c> in XAML.</summary>
    public TrackableValue<string> SolutionPath => _store.SolutionOptionsEditor.SolutionPath;

    /// <summary>Whether the Browse command stores the path relative to the project file.
    /// Bound via <c>UseRelativePath.Value</c> in XAML.</summary>
    public TrackableValue<bool> UseRelativePath => _store.SolutionOptionsEditor.UseRelativePath;

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>Interaction that asks the view to browse for a .sln/.slnx file.</summary>
    public Interaction<string, string?> BrowseSolutionPathInteraction { get; } = new();

    /// <summary>Command that opens a file browser for the solution path.</summary>
    public ReactiveCommand<Unit, Unit> BrowseSolutionPathCommand { get; }

    // Tag-input helpers for the four list editors.

    /// <summary>Tag-input state for the regex-to-include list.</summary>
    public TagInputModel RegexToIncludeInput { get; }

    /// <summary>Tag-input state for the regex-to-exclude list.</summary>
    public TagInputModel RegexToExcludeInput { get; }

    /// <summary>Tag-input state for the packages-to-exclude list.</summary>
    public TagInputModel PackagesToExcludeInput { get; }

    /// <summary>Tag-input state for the frameworks-to-exclude list.</summary>
    public TagInputModel FrameworksToExcludeInput { get; }

    /// <summary>Whether individual-project scope is enabled.</summary>
    public TrackableValue<bool> IndividualEnabled => _store.SolutionOptionsEditor.IndividualEnabled;

    /// <summary>Whether individual-project scope includes dependencies.</summary>
    public TrackableValue<bool> IndividualIncludeDependencies => _store.SolutionOptionsEditor.IndividualIncludeDependencies;

    /// <summary>Transitive depth for individual-project scope.</summary>
    public TrackableValue<int> IndividualTransitiveDepth => _store.SolutionOptionsEditor.IndividualTransitiveDepth;

    /// <summary>Whether all-projects scope is enabled.</summary>
    public TrackableValue<bool> AllEnabled => _store.SolutionOptionsEditor.AllEnabled;

    /// <summary>Whether all-projects scope includes dependencies.</summary>
    public TrackableValue<bool> AllIncludeDependencies => _store.SolutionOptionsEditor.AllIncludeDependencies;

    /// <summary>Transitive depth for all-projects scope.</summary>
    public TrackableValue<int> AllTransitiveDepth => _store.SolutionOptionsEditor.AllTransitiveDepth;

    /// <summary>Initializes a new instance of <see cref="SolutionViewModel"/>.</summary>
    /// <param name="store">The project document store providing the editing surface.</param>
    public SolutionViewModel(IProjectDocumentStore store)
    {
        _store = store;

        RegexToIncludeInput = new TagInputModel(_store.SolutionOptionsEditor.RegexToInclude.Value, TryValidateRegex);
        RegexToExcludeInput = new TagInputModel(_store.SolutionOptionsEditor.RegexToExclude.Value, TryValidateRegex);
        PackagesToExcludeInput = new TagInputModel(_store.SolutionOptionsEditor.PackagesToExclude.Value);
        FrameworksToExcludeInput = new TagInputModel(_store.SolutionOptionsEditor.FrameworksToExclude.Value);

        WireValidation();
        WireRelativePathToggle();
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

        this.ValidationRule(
            viewModel => viewModel.IndividualEnabled.Value,
            individualEnabled => individualEnabled || AllEnabled.Value,
            "At least one scope must be enabled.");

        this.ValidationRule(
            viewModel => viewModel.AllEnabled.Value,
            allEnabled => IndividualEnabled.Value || allEnabled,
            "At least one scope must be enabled.");
    }

    private void WireRelativePathToggle()
    {
        this.WhenAnyValue(vm => vm.UseRelativePath.Value)
            .Skip(1)        // Skip the initial seeded value after loading
            .Subscribe(useRelative =>
            {
                var currentPath = SolutionPath.Value;

                if (currentPath.IsNullOrEmpty())
                {
                    return;
                }

                var absolutePath = PathUtils.ResolveAsAbsolutePath(currentPath, _store.DocumentDirectory);

                SolutionPath.Value = useRelative
                    ? PathUtils.MakeRelativeIfPossible(absolutePath, _store.DocumentDirectory)
                    : absolutePath;
            });
    }

    private static string? TryValidateRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(200));
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }

    private ReactiveCommand<Unit, Unit> CreateBrowseCommand()
    {
        return ReactiveCommand.CreateFromObservable(() =>
        {
            var resolvedPath = PathUtils.ResolveAsAbsolutePath(SolutionPath.Value ?? string.Empty, _store.DocumentDirectory);

            return BrowseSolutionPathInteraction
                .Handle(resolvedPath)
                .Do(path =>
                {
                    if (path is not null)
                    {
                        SolutionPath.Value = UseRelativePath.Value
                            ? PathUtils.MakeRelativeIfPossible(path, _store.DocumentDirectory)
                            : path;
                    }
                })
                .Select(_ => Unit.Default);
        });
    }
}
