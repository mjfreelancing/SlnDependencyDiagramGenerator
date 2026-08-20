using AllOverIt.Extensions;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using SlnDependencyStudio.Shared.Utils;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Extensions;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Features.Pipeline;

/// <summary>View model for the "Pipeline" navigation page.</summary>
public sealed class PipelineViewModel : ReactiveObject, IValidatableViewModel, IDisposable
{
    /// <summary>The debounce delay applied to real-time working-directory validation while typing.</summary>
    private static readonly TimeSpan WorkingDirectoryValidationDelay = TimeSpan.FromMilliseconds(300);

    private readonly IProjectDocumentStore _store;
    private readonly IToolStatusService _toolStatus;
    private readonly IErrorDialogService _errorDialog;
    private readonly ILogger<PipelineViewModel> _logger;
    private readonly ObservableCollection<ToolStatusEntry> _toolStatusEntries;
    private readonly CompositeDisposable _disposables = [];

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

    /// <summary>When true, the pre-generation Browse button stores the working directory relative to the project file.
    /// Synced from the loaded working directory by the editor's <c>SetOriginalValues</c>.</summary>
    public TrackableValue<bool> UseRelativePathForPreGenWorkingDirectory => _store.PreGenerationEditor.UseRelativePath;

    /// <summary>Whether the solution should be restored (via <c>dotnet restore</c>) before generation.</summary>
    public TrackableValue<bool> RestoreSolution => _store.RestoreSolutionEditor.RestoreSolution;

    /// <summary>Whether the post-generation command is enabled.</summary>
    public TrackableValue<bool> PostGenEnabled => _store.PostGenerationEditor.Enabled;

    /// <summary>The command or executable path to run.</summary>
    public TrackableValue<string> PostGenCommand => _store.PostGenerationEditor.Command;

    /// <summary>Command-line arguments.</summary>
    public TrackableValue<string> PostGenArguments => _store.PostGenerationEditor.Arguments;

    /// <summary>The working directory for the command.</summary>
    public TrackableValue<string> PostGenWorkingDirectory => _store.PostGenerationEditor.WorkingDirectory;

    /// <summary>When true, the post-generation Browse button stores the working directory relative to the project file.
    /// Synced from the loaded working directory by the editor's <c>SetOriginalValues</c>.</summary>
    public TrackableValue<bool> UseRelativePathForPostGenWorkingDirectory => _store.PostGenerationEditor.UseRelativePath;

    /// <inheritdoc />
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    /// <summary>The current tool status entries (d2, mmdc).</summary>
    public ReadOnlyObservableCollection<ToolStatusEntry> ToolStatusEntries { get; }

    private string? _preGenError;

    /// <summary>
    /// Validation error for the pre-generation command, or <see langword="null"/>
    /// when valid. Only reports an error when the toggle is enabled and the command
    /// is empty.
    /// </summary>
    public string? PreGenError
    {
        get => _preGenError;
        private set => this.RaiseAndSetIfChanged(ref _preGenError, value);
    }

    private string? _postGenError;

    /// <summary>
    /// Validation error for the post-generation command, or <see langword="null"/>
    /// when valid. Only reports an error when the toggle is enabled and the command
    /// is empty.
    /// </summary>
    public string? PostGenError
    {
        get => _postGenError;
        private set => this.RaiseAndSetIfChanged(ref _postGenError, value);
    }

    private string? _preGenWorkingDirectoryError;

    /// <summary>
    /// Validation error for the pre-generation working directory, or <see langword="null"/>
    /// when valid. Only reports an error when the toggle is enabled, the directory is
    /// non-empty, and the resolved directory does not exist.
    /// </summary>
    public string? PreGenWorkingDirectoryError
    {
        get => _preGenWorkingDirectoryError;
        private set => this.RaiseAndSetIfChanged(ref _preGenWorkingDirectoryError, value);
    }

    private string? _postGenWorkingDirectoryError;

    /// <summary>
    /// Validation error for the post-generation working directory, or <see langword="null"/>
    /// when valid. Only reports an error when the toggle is enabled, the directory is
    /// non-empty, and the resolved directory does not exist.
    /// </summary>
    public string? PostGenWorkingDirectoryError
    {
        get => _postGenWorkingDirectoryError;
        private set => this.RaiseAndSetIfChanged(ref _postGenWorkingDirectoryError, value);
    }

    /// <summary>Interaction for browsing for an executable file.</summary>
    public Interaction<string, string?> BrowseCommandInteraction { get; } = new();

    /// <summary>Interaction for browsing for a working directory.</summary>
    public Interaction<string, string?> BrowseWorkingDirectoryInteraction { get; } = new();

    /// <summary>Command that opens a file browser for the command path.</summary>
    public ReactiveCommand<Unit, Unit> BrowseCommandCommand { get; }

    /// <summary>Command that opens a folder browser for the working directory.</summary>
    public ReactiveCommand<Unit, Unit> BrowseWorkingDirectoryCommand { get; }

    /// <summary>Command that opens a file browser for the post-generation command path.</summary>
    public ReactiveCommand<Unit, Unit> BrowsePostGenCommandCommand { get; }

    /// <summary>Command that opens a folder browser for the post-generation working directory.</summary>
    public ReactiveCommand<Unit, Unit> BrowsePostGenWorkingDirectoryCommand { get; }

    /// <summary>Command that triggers a tool re-scan.</summary>
    public ReactiveCommand<Unit, Unit> RescanToolsCommand { get; }

    /// <summary>Initializes a new instance of <see cref="PipelineViewModel"/>.</summary>
    /// <param name="store">The project document store.</param>
    /// <param name="toolStatus">The tool status service.</param>
    /// <param name="errorDialog">The error dialog service.</param>
    /// <param name="logger">The logger instance.</param>
    public PipelineViewModel(IProjectDocumentStore store, IToolStatusService toolStatus,
        IErrorDialogService errorDialog, ILogger<PipelineViewModel> logger)
    {
        _store = store;
        _toolStatus = toolStatus;
        _errorDialog = errorDialog;
        _logger = logger;

        _toolStatusEntries = [];
        ToolStatusEntries = new ReadOnlyObservableCollection<ToolStatusEntry>(_toolStatusEntries);

        WirePreGenError();
        WirePostGenError();
        WirePreGenWorkingDirectoryError();
        WirePostGenWorkingDirectoryError();
        WireValidation();
        WireToolStatus();
        WirePreGenRelativePathToggle();
        WirePostGenRelativePathToggle();

        BrowseCommandCommand = CreateBrowseCommandCommand(Command, WorkingDirectory);
        BrowseWorkingDirectoryCommand = CreateBrowseWorkingDirectoryCommand(WorkingDirectory, UseRelativePathForPreGenWorkingDirectory);
        BrowsePostGenCommandCommand = CreateBrowseCommandCommand(PostGenCommand, PostGenWorkingDirectory);
        BrowsePostGenWorkingDirectoryCommand = CreateBrowseWorkingDirectoryCommand(PostGenWorkingDirectory, UseRelativePathForPostGenWorkingDirectory);
        RescanToolsCommand = CreateRescanToolsCommand();

        // Route a failed tool rescan to the error dialog instead of the global fallback (message box).
        RescanToolsCommand
            .WireThrownExceptionsToErrorDialog(_errorDialog, "Tool Rescan failed", _logger)
            .DisposeWith(_disposables);
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
            .Subscribe(error => PreGenError = error)
            .DisposeWith(_disposables);
    }

    private void WirePostGenError()
    {
        this.WhenAnyValue(
                vm => vm.PostGenEnabled.Value,
                vm => vm.PostGenCommand.Value,
                (enabled, command) =>
                    enabled && command.IsNullOrEmpty()
                        ? "Command must not be empty when post-generation is enabled."
                        : null)
            .Subscribe(error => PostGenError = error)
            .DisposeWith(_disposables);
    }

    private void WirePreGenWorkingDirectoryError()
    {
        this.WhenAnyValue(
                vm => vm.Enabled.Value,
                vm => vm.WorkingDirectory.Value,
                vm => vm.UseRelativePathForPreGenWorkingDirectory.Value,
                (enabled, workingDirectory, useRelativePath) => new { enabled, workingDirectory, useRelativePath })
            .Throttle(WorkingDirectoryValidationDelay, RxSchedulers.MainThreadScheduler)
            .Select(item => ValidateWorkingDirectory(item.enabled, item.useRelativePath, item.workingDirectory, _store.DocumentDirectory))
            .Subscribe(error => PreGenWorkingDirectoryError = error)
            .DisposeWith(_disposables);
    }

    private void WirePostGenWorkingDirectoryError()
    {
        this.WhenAnyValue(
                vm => vm.PostGenEnabled.Value,
                vm => vm.PostGenWorkingDirectory.Value,
                vm => vm.UseRelativePathForPostGenWorkingDirectory.Value,
                (enabled, workingDirectory, useRelativePath) => new { enabled, workingDirectory, useRelativePath })
            .Throttle(WorkingDirectoryValidationDelay, RxSchedulers.MainThreadScheduler)
            .Select(item => ValidateWorkingDirectory(item.enabled, item.useRelativePath, item.workingDirectory, _store.DocumentDirectory))
            .Subscribe(error => PostGenWorkingDirectoryError = error)
            .DisposeWith(_disposables);
    }

    private static string? ValidateWorkingDirectory(bool enabled, bool useRelativePath, string? workingDirectory, string documentDirectory)
    {
        if (!enabled || workingDirectory.IsNullOrEmpty())
        {
            return null;
        }

        if (useRelativePath)
        {
            var resolvedPath = PathUtils.ResolveAsAbsolutePath(workingDirectory, documentDirectory);

            return Directory.Exists(resolvedPath)
                ? null
                : $"Working directory was not found: {resolvedPath}";
        }

        // Absolute mode — the value is validated as-is, without resolving against the project directory.
        return Directory.Exists(workingDirectory)
            ? null
            : $"Working directory was not found: {workingDirectory}";
    }

    private void WireValidation()
    {
        this.ValidationRule(
            viewModel => viewModel.PreGenError,
            error => error is null,
            error => error ?? string.Empty);

        this.ValidationRule(
            viewModel => viewModel.PostGenError,
            error => error is null,
            error => error ?? string.Empty);

        this.ValidationRule(
            viewModel => viewModel.PreGenWorkingDirectoryError,
            error => error is null,
            error => error ?? string.Empty);

        this.ValidationRule(
            viewModel => viewModel.PostGenWorkingDirectoryError,
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
            })
            .DisposeWith(_disposables);
    }

    private void WirePreGenRelativePathToggle()
    {
        // The editor's SetOriginalValues syncs UseRelativePath to the loaded working directory, so
        // the checkbox always reflects the stored format and the initial emission here is a no-op.
        // When the user flips the checkbox, ToggleRelativePath rewrites the working directory to match.
        this.WhenAnyValue(vm => vm.UseRelativePathForPreGenWorkingDirectory.Value)
            .Subscribe(useRelative => ToggleRelativePath(WorkingDirectory, useRelative))
            .DisposeWith(_disposables);
    }

    private void WirePostGenRelativePathToggle()
    {
        // Same as WirePreGenRelativePathToggle — the checkbox is synced on load and only converts
        // the working directory when the user flips it.
        this.WhenAnyValue(vm => vm.UseRelativePathForPostGenWorkingDirectory.Value)
            .Subscribe(useRelative => ToggleRelativePath(PostGenWorkingDirectory, useRelative))
            .DisposeWith(_disposables);
    }

    private void ToggleRelativePath(TrackableValue<string> workingDirectory, bool useRelative)
    {
        var path = workingDirectory.Value;

        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        var docPath = _store.DocumentFilePath;

        if (docPath is null)
        {
            return;
        }

        var docDir = Path.GetDirectoryName(docPath);

        if (docDir is null)
        {
            return;
        }

        var isCurrentlyRelative = !Path.IsPathFullyQualified(path);

        if (useRelative)
        {
            // Only convert if currently absolute — relative paths are already correct.
            if (!isCurrentlyRelative)
            {
                workingDirectory.Value = Path.GetRelativePath(docDir, path);
            }
        }
        else
        {
            // Only convert if currently relative — absolute paths are already correct.
            if (isCurrentlyRelative)
            {
                workingDirectory.Value = Path.GetFullPath(path, docDir);
            }
        }
    }

    private ReactiveCommand<Unit, Unit> CreateRescanToolsCommand()
    {
        return ReactiveCommand.CreateFromTask(_toolStatus.RescanAsync);
    }

    private ReactiveCommand<Unit, Unit> CreateBrowseCommandCommand(TrackableValue<string> command, TrackableValue<string> workingDirectory)
    {
        return ReactiveCommand.CreateFromObservable(() =>
        {
            return BrowseCommandInteraction
                .Handle(command.Value ?? string.Empty)
                .Do(fullPath =>
                {
                    if (fullPath is null)
                    {
                        return;
                    }

                    command.Value = Path.GetFileName(fullPath);

                    if (workingDirectory.Value.IsNullOrEmpty())
                    {
                        var directory = Path.GetDirectoryName(fullPath);

                        if (directory is not null)
                        {
                            workingDirectory.Value = directory;
                        }
                    }
                })
                .Select(_ => Unit.Default);
        });
    }

    private ReactiveCommand<Unit, Unit> CreateBrowseWorkingDirectoryCommand(TrackableValue<string> workingDirectory, TrackableValue<bool> useRelativePath)
    {
        return ReactiveCommand.CreateFromObservable(() =>
        {
            var resolvedPath = PathUtils.ResolveAsAbsolutePath(workingDirectory.Value, _store.DocumentDirectory);

            return BrowseWorkingDirectoryInteraction
                .Handle(resolvedPath)
                .Do(path =>
                {
                    if (path is null)
                    {
                        return;
                    }

                    workingDirectory.Value = ResolveBrowsedWorkingDirectory(path, useRelativePath.Value);
                })
                .Select(_ => Unit.Default);
        });
    }

    private string ResolveBrowsedWorkingDirectory(string path, bool useRelative)
    {
        if (!useRelative)
        {
            return path;
        }

        var docPath = _store.DocumentFilePath;

        if (docPath is null)
        {
            return path;
        }

        var docDir = Path.GetDirectoryName(docPath);

        if (docDir is null)
        {
            return path;
        }

        return Path.GetRelativePath(docDir, path);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
