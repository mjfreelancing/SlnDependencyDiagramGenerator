using AllOverIt.Extensions;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Pipeline;

/// <summary>View model for the "Pipeline" navigation page.</summary>
public sealed class PipelineViewModel : ReactiveObject, IValidatableViewModel
{
    private readonly IProjectDocumentStore _store;
    private readonly IToolStatusService _toolStatus;
    private readonly ObservableCollection<ToolStatusEntry> _toolStatusEntries;

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

    /// <summary>When true, the Browse button stores the working directory relative to the project file.</summary>
    public TrackableValue<bool> UseRelativePathForWorkingDirectory { get; } = new();

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>The current tool status entries (d2, mmdc).</summary>
    public ReadOnlyObservableCollection<ToolStatusEntry> ToolStatusEntries { get; }

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

    /// <summary>Command that triggers a tool re-scan.</summary>
    public ReactiveCommand<Unit, Unit> RescanToolsCommand { get; }

    /// <summary>Initializes a new instance of <see cref="PipelineViewModel"/>.</summary>
    /// <param name="store">The project document store.</param>
    /// <param name="toolStatus">The tool status service.</param>
    public PipelineViewModel(IProjectDocumentStore store, IToolStatusService toolStatus)
    {
        _store = store;
        _toolStatus = toolStatus;

        UseRelativePathForWorkingDirectory.SetOriginalValue(true);

        _toolStatusEntries = [];
        ToolStatusEntries = new ReadOnlyObservableCollection<ToolStatusEntry>(_toolStatusEntries);

        WirePreGenError();
        WireValidation();
        WireToolStatus();
        WireRelativePathToggle();

        BrowseCommandCommand = CreateBrowseCommandCommand();
        BrowseWorkingDirectoryCommand = CreateBrowseWorkingDirectoryCommand();
        RescanToolsCommand = CreateRescanToolsCommand();
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

    private void WireToolStatus()
    {
        _toolStatus
            .ToolStatuses
            .Subscribe(entries =>
            {
                _toolStatusEntries.Clear();

                for (var i = 0; i < entries.Count; i++)
                {
                    _toolStatusEntries.Add(entries[i]);
                }
            });
    }

    private void WireRelativePathToggle()
    {
        this.WhenAnyValue(vm => vm.UseRelativePathForWorkingDirectory.Value)
            .Subscribe(useRelative =>
            {
                var path = WorkingDirectory.Value;

                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                var docPath = _store.DocumentFilePath;

                if (docPath is null)
                {
                    return;
                }

                var docDir = System.IO.Path.GetDirectoryName(docPath);

                if (docDir is null)
                {
                    return;
                }

                var isCurrentlyRelative = !System.IO.Path.IsPathFullyQualified(path);

                if (useRelative)
                {
                    // Only convert if currently absolute — relative paths are already correct.
                    if (!isCurrentlyRelative)
                    {
                        WorkingDirectory.Value = System.IO.Path.GetRelativePath(docDir, path);
                    }
                }
                else
                {
                    // Only convert if currently relative — absolute paths are already correct.
                    if (isCurrentlyRelative)
                    {
                        WorkingDirectory.Value = System.IO.Path.GetFullPath(path, docDir);
                    }
                }
            });
    }

    private ReactiveCommand<Unit, Unit> CreateRescanToolsCommand()
    {
        return ReactiveCommand.CreateFromTask(async ct =>
        {
            await _toolStatus.RescanAsync(ct);
        });
    }

    private ReactiveCommand<Unit, Unit> CreateBrowseCommandCommand()
    {
        return ReactiveCommand.CreateFromObservable(() =>
        {
            return BrowseCommandInteraction
                .Handle(Command.Value ?? string.Empty)
                .Do(fullPath =>
                {
                    if (fullPath is null)
                    {
                        return;
                    }

                    Command.Value = System.IO.Path.GetFileName(fullPath);

                    if (WorkingDirectory.Value.IsNullOrEmpty())
                    {
                        var directory = System.IO.Path.GetDirectoryName(fullPath);

                        if (directory is not null)
                        {
                            WorkingDirectory.Value = directory;
                        }
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
                    if (path is null)
                    {
                        return;
                    }

                    if (UseRelativePathForWorkingDirectory.Value)
                    {
                        var docPath = _store.DocumentFilePath;

                        if (docPath is not null)
                        {
                            var docDir = System.IO.Path.GetDirectoryName(docPath);

                            if (docDir is not null)
                            {
                                path = System.IO.Path.GetRelativePath(docDir, path);
                            }
                        }
                    }

                    WorkingDirectory.Value = path;
                })
                .Select(_ => Unit.Default);
        });
    }
}
