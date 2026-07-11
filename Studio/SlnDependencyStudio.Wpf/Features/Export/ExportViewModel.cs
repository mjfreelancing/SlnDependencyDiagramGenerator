using AllOverIt.Extensions;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Export.Models;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Export;

/// <summary>View model for the "Export" navigation page. Delegates all document-related
/// bindings to the <see cref="IProjectDocumentStore"/> so that the store is the single
/// source of truth for the currently open document.</summary>
public sealed class ExportViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;
    private bool _isSyncing;

    /// <summary>The export root path. Bound via <c>RootPath.Value</c> in XAML.</summary>
    public TrackableValue<string> RootPath => _store.ExportOptionsEditor.RootPath;

    /// <summary>Whether the Browse command stores the path relative to the project file.
    /// Bound via <c>UseRelativePath.Value</c> in XAML.</summary>
    public TrackableValue<bool> UseRelativePath => _store.ExportOptionsEditor.UseRelativePath;

    /// <summary>Whether to clear the output folder before generating files.
    /// Bound via <c>ClearContents.Value</c> in XAML.</summary>
    public TrackableValue<bool> ClearContents => _store.ExportOptionsEditor.ClearContents;

    /// <summary>The image formats collection. Items are added/removed as toggles change.</summary>
    public TrackableCollection<DiagramImageFormat> ImageFormats => _store.ExportOptionsEditor.ImageFormats;

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>Interaction that asks the view to browse for an export folder.</summary>
    public Interaction<string, string?> BrowseExportPathInteraction { get; } = new();

    /// <summary>Command that opens a folder browser for the export root path.</summary>
    public ReactiveCommand<Unit, Unit> BrowseExportPathCommand { get; }

    /// <summary>The image format toggle items bound to the ItemsControl.</summary>
    public ReadOnlyObservableCollection<ImageFormatToggleItem> ImageFormatToggles { get; }

    /// <summary>Initializes a new instance of <see cref="ExportViewModel"/>.</summary>
    /// <param name="store">The project document store providing the editing surface.</param>
    public ExportViewModel(IProjectDocumentStore store)
    {
        _store = store;

        ImageFormatToggles = CreateImageFormatToggles();

        WireValidation();
        WireRelativePathToggle();
        WireImageFormatSync();
        BrowseExportPathCommand = CreateBrowseCommand();
    }

    private static ReadOnlyObservableCollection<ImageFormatToggleItem> CreateImageFormatToggles()
    {
        var toggles = Enum.GetValues<DiagramImageFormat>()
            .Select(format => new ImageFormatToggleItem
            {
                Format = format,
                DisplayName = GetDisplayName(format),
                IconKind = GetIconKind(format)
            })
            .ToList();

        return new ReadOnlyObservableCollection<ImageFormatToggleItem>(
            new ObservableCollection<ImageFormatToggleItem>(toggles));
    }

    private void WireImageFormatSync()
    {
        ImageFormats.Items.CollectionChanged += OnImageFormatsCollectionChanged;

        SyncImageFormatTogglesFromCollection();

        foreach (var toggle in ImageFormatToggles)
        {
            toggle
                .WhenAnyValue(toggleItem => toggleItem.IsChecked)
                .Subscribe(isChecked => OnImageFormatToggleChanged(toggle, isChecked));
        }
    }

    private void SyncImageFormatTogglesFromCollection()
    {
        _isSyncing = true;

        foreach (var toggle in ImageFormatToggles)
        {
            toggle.IsChecked = ImageFormats.Items.Contains(toggle.Format);
        }

        _isSyncing = false;
    }

    private void OnImageFormatsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isSyncing)
        {
            return;
        }

        SyncImageFormatTogglesFromCollection();
    }

    private void OnImageFormatToggleChanged(ImageFormatToggleItem toggle, bool isChecked)
    {
        if (_isSyncing)
        {
            return;
        }

        _isSyncing = true;

        if (isChecked && !ImageFormats.Items.Contains(toggle.Format))
        {
            ImageFormats.Items.Add(toggle.Format);
        }
        else if (!isChecked)
        {
            ImageFormats.Items.Remove(toggle.Format);
        }

        _isSyncing = false;
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

    private static string GetDisplayName(DiagramImageFormat format)
    {
        return format switch
        {
            DiagramImageFormat.Png => "PNG",
            DiagramImageFormat.Svg => "SVG",
            DiagramImageFormat.Pdf => "PDF",
            _ => format.ToString()
        };
    }

    private static MaterialDesignThemes.Wpf.PackIconKind GetIconKind(DiagramImageFormat format)
    {
        return format switch
        {
            DiagramImageFormat.Png => MaterialDesignThemes.Wpf.PackIconKind.ImageOutline,
            DiagramImageFormat.Svg => MaterialDesignThemes.Wpf.PackIconKind.VectorSquare,
            DiagramImageFormat.Pdf => MaterialDesignThemes.Wpf.PackIconKind.FilePdfBox,
            _ => MaterialDesignThemes.Wpf.PackIconKind.FileImage
        };
    }
}
