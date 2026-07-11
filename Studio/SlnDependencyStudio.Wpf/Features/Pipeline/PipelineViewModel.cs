using AllOverIt.Extensions;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Pipeline;

/// <summary>View model for the "Pipeline" navigation page.</summary>
public sealed class PipelineViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;

    /// <summary>Whether the pre-generation command is enabled.</summary>
    public TrackableValue<bool> Enabled => _store.PreGenerationEditor.Enabled;

    /// <summary>The command or executable path to run.</summary>
    public TrackableValue<string> Command => _store.PreGenerationEditor.Command;

    /// <summary>Command-line arguments.</summary>
    public TrackableValue<string> Arguments => _store.PreGenerationEditor.Arguments;

    /// <summary>The working directory for the command.</summary>
    public TrackableValue<string> WorkingDirectory => _store.PreGenerationEditor.WorkingDirectory;

    /// <summary>Whether to continue on failure.</summary>
    public TrackableValue<bool> ContinueOnFailure => _store.PreGenerationEditor.ContinueOnFailure;

    /// <summary>When true, the Browse command stores the command path relative to the project file.</summary>
    public TrackableValue<bool> UseRelativePathForCommand { get; } = new();

    /// <summary>When true, the Browse command stores the working directory relative to the project file.</summary>
    public TrackableValue<bool> UseRelativePathForWorkingDirectory { get; } = new();

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    private string? _preGenError;

    /// <summary>
    /// Validation error for the pre-generation command, or <see langword="null"/>
    /// when valid. Only reports an error when the toggle is enabled and the command
    /// is empty.</c>.
    /// </summary>
    public string? PreGenError
    {
        get => _preGenError;
        private set => this.RaiseAndSetIfChanged(ref _preGenError, value);
    }

    /// <summary>Interaction for browsing for an executable file.</summary>
    public Interaction<string, string?> BrowseCommandInteraction { get; } = new();

    /// <summary>Interaction for browsing for a working directory.</summary>
    public Interaction<string, string?> BrowseWorkingDirectoryInteraction { get; } = new();

    /// <summary>Command that opens a file browser for the command path.</summary>
    public ReactiveCommand<Unit, Unit> BrowseCommandCommand { get; }

    /// <summary>Command that opens a folder browser for the working directory.</summary>
    public ReactiveCommand<Unit, Unit> BrowseWorkingDirectoryCommand { get; }

    /// <summary>Initializes a new instance of <see cref="PipelineViewModel"/>.</summary>
    /// <param name="store">The project document store.</param>
    public PipelineViewModel(IProjectDocumentStore store)
    {
        _store = store;

        UseRelativePathForCommand.SetOriginalValue(true);
        UseRelativePathForWorkingDirectory.SetOriginalValue(true);

        WirePreGenError();
        WireValidation();
        BrowseCommandCommand = CreateBrowseCommandCommand();
        BrowseWorkingDirectoryCommand = CreateBrowseWorkingDirectoryCommand();
    }

    private void WirePreGenError()
    {
        this.WhenAnyValue(
                vm => vm.Enabled.Value,
                vm => vm.Command.Value,
                (enabled, command) =>
                    enabled && command.IsNullOrEmpty()
                        ? "Command must not be empty when pre-generation is enabled."
                        : null)
            .Subscribe(error => PreGenError = error);
    }

    private void WireValidation()
    {
        this.ValidationRule(
            viewModel => viewModel.PreGenError,
            error => error is null,
            error => error ?? string.Empty);
    }

    private ReactiveCommand<Unit, Unit> CreateBrowseCommandCommand()
    {
        return ReactiveCommand.CreateFromObservable(() =>
        {
            return BrowseCommandInteraction
                .Handle(Command.Value ?? string.Empty)
                .Do(path =>
                {
                    if (path is not null)
                    {
                        Command.Value = path;
                    }
                })
                .Select(_ => Unit.Default);
        });
    }

    private ReactiveCommand<Unit, Unit> CreateBrowseWorkingDirectoryCommand()
    {
        return ReactiveCommand.CreateFromObservable(() =>
        {
            return BrowseWorkingDirectoryInteraction
                .Handle(WorkingDirectory.Value ?? string.Empty)
                .Do(path =>
                {
                    if (path is not null)
                    {
                        WorkingDirectory.Value = path;
                    }
                })
                .Select(_ => Unit.Default);
        });
    }
}
