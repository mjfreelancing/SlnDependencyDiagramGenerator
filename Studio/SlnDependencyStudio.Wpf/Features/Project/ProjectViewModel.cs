using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Project;

/// <summary>View model for the "Project" navigation page. Delegates all document-related
/// bindings to the <see cref="IProjectDocumentStore"/> so that the store is the single
/// source of truth for the currently open document.</summary>
public sealed class ProjectViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;
    private string? _documentFilePath;

    /// <summary>The project name. Bound via <c>ProjectName.Value</c> in XAML.</summary>
    public TrackableValue<string> ProjectName => _store.MetadataEditor.ProjectName;

    /// <summary>The project description. Bound via <c>Description.Value</c> in XAML.</summary>
    public TrackableValue<string> Description => _store.MetadataEditor.Description;

    /// <summary>The full path to the currently open document, or <see langword="null"/> when no document is open.</summary>
    public string? DocumentFilePath
    {
        get => _documentFilePath;
        private set => this.RaiseAndSetIfChanged(ref _documentFilePath, value);
    }

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>Initializes a new instance of <see cref="ProjectViewModel"/>.</summary>
    /// <param name="store">The project document store providing the editing surface.</param>
    public ProjectViewModel(IProjectDocumentStore store)
    {
        _store = store;

        _store
            .WhenAnyValue(s => s.DocumentFilePath)
            .Subscribe(path => DocumentFilePath = path);

        this.ValidationRule(
            viewModel => viewModel.ProjectName.Value,
            name => !string.IsNullOrWhiteSpace(name),
            "Project name must not be empty");
    }
}
