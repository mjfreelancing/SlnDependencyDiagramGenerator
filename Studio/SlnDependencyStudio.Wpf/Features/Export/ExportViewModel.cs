using AllOverIt.Extensions;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Export;

/// <summary>View model for the "Export" navigation page. Delegates all document-related
/// bindings to the <see cref="IProjectDocumentStore"/> so that the store is the single
/// source of truth for the currently open document.</summary>
public sealed class ExportViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;

    /// <summary>The export root path. Bound via <c>RootPath.Value</c> in XAML.</summary>
    public TrackableValue<string> RootPath => _store.ExportOptionsEditor.RootPath;

    /// <summary>Whether the Browse command stores the path relative to the project file.
    /// Bound via <c>UseRelativePath.Value</c> in XAML.</summary>
    public TrackableValue<bool> UseRelativePath => _store.ExportOptionsEditor.UseRelativePath;

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>Interaction that asks the view to browse for an export folder.</summary>
    public Interaction<string, string?> BrowseExportPathInteraction { get; } = new();

    /// <summary>Command that opens a folder browser for the export root path.</summary>
    public ReactiveCommand<Unit, Unit> BrowseExportPathCommand { get; }

    /// <summary>Initializes a new instance of <see cref="ExportViewModel"/>.</summary>
    /// <param name="store">The project document store providing the editing surface.</param>
    public ExportViewModel(IProjectDocumentStore store)
    {
        _store = store;

        WireValidation();
        WireRelativePathToggle();
        BrowseExportPathCommand = CreateBrowseCommand();
    }

    private void WireRelativePathToggle()
    {
        this.WhenAnyValue(vm => vm.UseRelativePath.Value)
            .Skip(1)        // Skip the initial seeded value after loading
            .Subscribe(useRelative =>
            {
                var currentPath = RootPath.Value;

                if (currentPath.IsNullOrEmpty())
                {
                    return;
                }

                var absolutePath = PathUtils.ResolveAsAbsolutePath(currentPath, _store.DocumentDirectory);

                RootPath.Value = useRelative
                    ? PathUtils.MakeRelativeIfPossible(absolutePath, _store.DocumentDirectory)
                    : absolutePath;
            });
    }

    private void WireValidation()
    {
        this.ValidationRule(
            viewModel => viewModel.RootPath.Value,
            path => path.IsNotNullOrEmpty(),
            "Export root path must not be empty.");
    }

    private ReactiveCommand<Unit, Unit> CreateBrowseCommand() =>
        ReactiveCommand.CreateFromObservable(() =>
        {
            var sdsDirectory = _store.DocumentDirectory;
            var resolvedPath = PathUtils.ResolveAsAbsolutePath(RootPath.Value ?? string.Empty, sdsDirectory);

            return BrowseExportPathInteraction
                .Handle(resolvedPath)
                .Do(path =>
                {
                    if (path is not null)
                    {
                        RootPath.Value = UseRelativePath.Value
                            ? PathUtils.MakeRelativeIfPossible(path, sdsDirectory)
                            : path;
                    }
                })
                .Select(_ => Unit.Default);
        });
}
