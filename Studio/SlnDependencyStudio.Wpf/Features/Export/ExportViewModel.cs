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

namespace SlnDependencyStudio.Wpf.Features.Export;

/// <summary>View model for the "Export" navigation page. Delegates all document-related
/// bindings to the <see cref="IProjectDocumentStore"/> so that the store is the single
/// source of truth for the currently open document.</summary>
public sealed class ExportViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;

    /// <summary>The export root path. Bound via <c>RootPath.Value</c> in XAML.</summary>
    public TrackableValue<string> RootPath => _store.ExportOptionsEditor.RootPath;

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
        BrowseExportPathCommand = CreateBrowseCommand();
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
                .Do(result =>
                {
                    if (result is not null)
                    {
                        RootPath.Value = PathUtils.MakeRelativeIfPossible(result, sdsDirectory);
                    }
                })
                .Select(_ => Unit.Default);
        });
}
