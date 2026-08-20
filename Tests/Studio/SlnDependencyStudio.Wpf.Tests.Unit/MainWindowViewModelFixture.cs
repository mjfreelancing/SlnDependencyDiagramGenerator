using AllOverIt.ReactiveUI.Factories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReactiveUI;
using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Shared.Logging;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Editors;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.EmptyState;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.PreGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using SlnDependencyStudio.Wpf.Features.Run;
using SlnDependencyStudio.Wpf.Enumerations;
using SlnDependencyStudio.Wpf.Features.Solution;
using SlnDependencyStudio.Wpf.Models;
using SlnDependencyStudio.Wpf.Utils;
using System.IO;
using System.Reactive.Linq;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;

namespace SlnDependencyStudio.Wpf.Tests.Unit;

[Collection(nameof(ReactiveUIInitializer))]
public class MainWindowViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IDependencyProjectService _projectService = Substitute.For<IDependencyProjectService>();
    private readonly IRecentProjectsStore _recentProjects = Substitute.For<IRecentProjectsStore>();
    private readonly IErrorDialogService _errorDialog;
    private readonly IViewFactory _viewFactory = Substitute.For<IViewFactory>();
    private readonly IPreGenerationAnalysisService _analysisService = Substitute.For<IPreGenerationAnalysisService>();
    private readonly IGenerationService _generationService = Substitute.For<IGenerationService>();
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelFixture()
        : this(Substitute.For<IErrorDialogService>())
    {
    }

    protected MainWindowViewModelFixture(IErrorDialogService errorDialog)
    {
        _errorDialog = errorDialog;

        var logger = Substitute.For<ILogger<MainWindowViewModel>>();

        // Set up the view factory to return a valid OutputPanelViewModel view.
        var appSettings = Substitute.For<IApplicationSettingsService>();

        var applicationSettings = new ApplicationSettings();
        appSettings.CurrentSettings.Returns(applicationSettings);
        appSettings.CurrentState.Returns(new ApplicationState());

        var outputPanelViewModel = new OutputPanelViewModel(
            new StudioLogBuffer(),
            appSettings,
            Substitute.For<IFileSystem>(),
            Substitute.For<IErrorDialogService>(),
            Substitute.For<ILogger<OutputPanelViewModel>>());

        var outputPanelView = Substitute.For<IViewFor<OutputPanelViewModel>>();
        outputPanelView.ViewModel.Returns(outputPanelViewModel);

        _viewFactory.CreateViewFor<OutputPanelViewModel>().Returns(outputPanelView);

        // Ensure CreateViewFor returns a valid IViewFor for navigation page types.
        _viewFactory.CreateViewFor<ProjectViewModel>().Returns(CreateMockView<ProjectViewModel>());
        _viewFactory.CreateViewFor<SolutionViewModel>().Returns(CreateMockView<SolutionViewModel>());
        _viewFactory.CreateViewFor<ExportViewModel>().Returns(CreateMockView<ExportViewModel>());
        _viewFactory.CreateViewFor<DiagramsViewModel>().Returns(CreateMockView<DiagramsViewModel>());
        _viewFactory.CreateViewFor<PipelineViewModel>().Returns(CreateMockView<PipelineViewModel>());

        // EmptyStateViewModel is sealed — use a real instance, not a substitute.
        // It reads RecentProjects from the store directly, so set up the store's
        // collection before creating the view model.
        _recentProjects.RecentProjects.Returns([]);
        _recentProjects.HasRecentProjects.Returns(false);

        var emptyStateViewModel = new EmptyStateViewModel(_recentProjects);

        var emptyStateView = Substitute.For<IViewFor<EmptyStateViewModel>>();
        emptyStateView.ViewModel.Returns(emptyStateViewModel);

        _viewFactory.CreateViewFor<EmptyStateViewModel>().Returns(emptyStateView);

        _viewModel = new MainWindowViewModel(
            _store, _projectService, _recentProjects, _errorDialog, _viewFactory,
            _analysisService, _generationService, logger);

        _store.HasDocument.Returns(true);
        _store.GetRelativePathFields().Returns([]);
    }

    public class SaveAsync : MainWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Delegate_To_Store()
        {
            _store.IsDirty.Returns(true);

            await _viewModel.SaveCommand.Execute();

            await _store.Received(1).SaveAsync(Arg.Any<CancellationToken>());
        }
    }

    public class SaveAsAsync : MainWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Show_Dialog_And_Save_To_New_Path()
        {
            _store.IsDirty.Returns(true);

            _viewModel.SaveFileInteraction.RegisterHandler(context =>
            {
                context.Input.ShouldBe("Studio Project files (*.sds)|*.sds|All files (*.*)|*.*");
                context.SetOutput("new.sds");
            });

            await _viewModel.SaveAsCommand.Execute();

            await _store.Received(1).SaveAsAsync("new.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_And_Convert_When_Destination_Folder_Differs()
        {
            _store.IsDirty.Returns(true);
            _store.DocumentDirectory.Returns(@"C:\projects");
            _store.GetRelativePathFields().Returns([new RelativePathField("Solution path", @"..\MyApp.sln")]);

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(@"D:\backup\new.sds"));
            _viewModel.RelativePathSaveAsInteraction.RegisterHandler(context =>
            {
                context.Input.RelativePaths.Count.ShouldBe(1);
                context.SetOutput(SaveAsRelativePathAction.ConvertToAbsolute);
            });

            await _viewModel.SaveAsCommand.Execute();

            await _store.Received(1).SaveAsAsync(@"D:\backup\new.sds", SaveAsRelativePathAction.ConvertToAbsolute, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Not_Save_When_Relative_Path_Prompt_Cancelled()
        {
            _store.IsDirty.Returns(true);
            _store.DocumentDirectory.Returns(@"C:\projects");
            _store.GetRelativePathFields().Returns([new RelativePathField("Solution path", @"..\MyApp.sln")]);

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(@"D:\backup\new.sds"));
            _viewModel.RelativePathSaveAsInteraction.RegisterHandler(context => context.SetOutput(SaveAsRelativePathAction.Cancel));

            await _viewModel.SaveAsCommand.Execute();

            await _store.DidNotReceive().SaveAsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _store.DidNotReceive().SaveAsAsync(Arg.Any<string>(), Arg.Any<SaveAsRelativePathAction>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Not_Prompt_When_Destination_Folder_Matches()
        {
            _store.IsDirty.Returns(true);
            _store.DocumentDirectory.Returns(@"C:\projects");
            _store.GetRelativePathFields().Returns([new RelativePathField("Solution path", @"..\MyApp.sln")]);

            var prompted = false;
            _viewModel.RelativePathSaveAsInteraction.RegisterHandler(context =>
            {
                prompted = true;
                context.SetOutput(SaveAsRelativePathAction.Cancel);
            });

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(@"C:\projects\copy.sds"));

            await _viewModel.SaveAsCommand.Execute();

            prompted.ShouldBeFalse();
            await _store.Received(1).SaveAsAsync(@"C:\projects\copy.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Not_Prompt_When_No_Relative_Paths()
        {
            _store.IsDirty.Returns(true);
            _store.DocumentDirectory.Returns(@"C:\projects");

            var prompted = false;
            _viewModel.RelativePathSaveAsInteraction.RegisterHandler(context =>
            {
                prompted = true;
                context.SetOutput(SaveAsRelativePathAction.Cancel);
            });

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(@"D:\backup\new.sds"));

            await _viewModel.SaveAsCommand.Execute();

            prompted.ShouldBeFalse();
            await _store.Received(1).SaveAsAsync(@"D:\backup\new.sds", Arg.Any<CancellationToken>());
        }
    }

    public class CloseProjectAsync : MainWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Close_Store_When_Clean()
        {
            _store.IsDirty.Returns(false);

            await _viewModel.CloseProjectCommand.Execute();

            _store.Received(1).Close();
            _viewModel.CurrentPage.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Saves()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            var projectName = CreateTrackableValue("MyProject");
            metadataEditor.ProjectName.Returns(projectName);
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context =>
            {
                context.Input.ShouldBe("MyProject");
                context.SetOutput(DiscardAction.Save);
            });

            await _viewModel.CloseProjectCommand.Execute();

            await _store.Received(1).SaveAsync(Arg.Any<CancellationToken>());
            _store.Received(1).Close();
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Discards()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("MyProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Discard));

            await _viewModel.CloseProjectCommand.Execute();

            _store.Received(1).Close();
            await _store.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Cancels()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("MyProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Cancel));

            await _viewModel.CloseProjectCommand.Execute();

            _store.DidNotReceive().Close();
            await _store.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
        }
    }

    public class OpenProjectAsync : MainWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Open_File()
        {
            _store.IsDirty.Returns(false);

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput("test.sds"));

            await _viewModel.OpenProjectCommand.Execute();

            await _store.Received(1).OpenAsync("test.sds", Arg.Any<CancellationToken>());
        }
    }

    public class NewProjectAsync : MainWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Prompt_Save_Dialog_Save_And_Open()
        {
            _store.IsDirty.Returns(false);

            var document = new DependencyProjectDocument();
            _projectService.CreateFromDefaults().Returns(document);

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("new.sds"));

            await _viewModel.NewProjectCommand.Execute();

            await _projectService.Received(1).SaveAsync(document, "new.sds", Arg.Any<CancellationToken>());
            await _store.Received(1).OpenAsync("new.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Saves_First()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("OldProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Save));

            var document = new DependencyProjectDocument();
            _projectService.CreateFromDefaults().Returns(document);

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("new.sds"));

            await _viewModel.NewProjectCommand.Execute();

            await _store.Received(1).SaveAsync(Arg.Any<CancellationToken>());
            await _projectService.Received(1).SaveAsync(document, "new.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Discards()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("OldProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Discard));

            var document = new DependencyProjectDocument();
            _projectService.CreateFromDefaults().Returns(document);

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("new.sds"));

            await _viewModel.NewProjectCommand.Execute();

            await _store.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
            await _projectService.Received(1).SaveAsync(document, "new.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Cancels()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("OldProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Cancel));

            await _viewModel.NewProjectCommand.Execute();

            await _store.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
            _projectService.DidNotReceive().CreateFromDefaults();
        }

        [Fact]
        public async Task Should_Do_Nothing_When_Save_Dialog_Cancelled()
        {
            _store.IsDirty.Returns(false);

            var document = new DependencyProjectDocument();
            _projectService.CreateFromDefaults().Returns(document);

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(null));

            await _viewModel.NewProjectCommand.Execute();

            await _projectService.DidNotReceive().SaveAsync(Arg.Any<DependencyProjectDocument>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _store.DidNotReceive().OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
    }

    public class NewFromExistingAsync : MainWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Prompt_Source_Destination_Save_And_Open()
        {
            _store.IsDirty.Returns(false);

            var document = new DependencyProjectDocument();
            _projectService.OpenAsync("source.sds", Arg.Any<CancellationToken>()).Returns(document);

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput("source.sds"));
            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("dest.sds"));

            await _viewModel.NewFromExistingCommand.Execute();

            await _projectService.Received(1).OpenAsync("source.sds", Arg.Any<CancellationToken>());
            await _projectService.Received(1).SaveAsync(document, "dest.sds", Arg.Any<CancellationToken>());
            await _store.Received(1).OpenAsync("dest.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Do_Nothing_When_Source_Dialog_Cancelled()
        {
            _store.IsDirty.Returns(false);

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput(null));

            await _viewModel.NewFromExistingCommand.Execute();

            await _projectService.DidNotReceive().OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Do_Nothing_When_Destination_Dialog_Cancelled()
        {
            _store.IsDirty.Returns(false);

            var document = new DependencyProjectDocument();
            _projectService.OpenAsync("source.sds", Arg.Any<CancellationToken>()).Returns(document);

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput("source.sds"));
            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(null));

            await _viewModel.NewFromExistingCommand.Execute();

            await _projectService.Received(1).OpenAsync("source.sds", Arg.Any<CancellationToken>());
            await _projectService.DidNotReceive().SaveAsync(Arg.Any<DependencyProjectDocument>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Cancels()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("OldProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Cancel));

            await _viewModel.NewFromExistingCommand.Execute();

            await _projectService.DidNotReceive().OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Saves_First()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("OldProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Save));

            var document = new DependencyProjectDocument();
            _projectService.OpenAsync("source.sds", Arg.Any<CancellationToken>()).Returns(document);

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput("source.sds"));
            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("dest.sds"));

            await _viewModel.NewFromExistingCommand.Execute();

            await _store.Received(1).SaveAsync(Arg.Any<CancellationToken>());
            await _projectService.Received(1).SaveAsync(document, "dest.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Discards()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("OldProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Discard));

            var document = new DependencyProjectDocument();
            _projectService.OpenAsync("source.sds", Arg.Any<CancellationToken>()).Returns(document);

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput("source.sds"));
            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("dest.sds"));

            await _viewModel.NewFromExistingCommand.Execute();

            await _store.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
            await _projectService.Received(1).SaveAsync(document, "dest.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Rebase_Relative_Paths_When_Destination_Folder_Differs()
        {
            _store.IsDirty.Returns(false);

            var document = new DependencyProjectDocument();
            document.DiagramGenerator.Solution.SolutionPath = @"..\MyApp.sln";

            _projectService.OpenAsync(@"C:\projects\source.sds", Arg.Any<CancellationToken>()).Returns(document);

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput(@"C:\projects\source.sds"));
            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(@"D:\backup\dest.sds"));
            _viewModel.RelativePathSaveAsInteraction.RegisterHandler(context =>
            {
                context.Input.RelativePaths.Count.ShouldBe(1);
                context.SetOutput(SaveAsRelativePathAction.ConvertToAbsolute);
            });

            await _viewModel.NewFromExistingCommand.Execute();

            await _projectService.Received(1).SaveAsync(
                Arg.Is<DependencyProjectDocument>(d => d.DiagramGenerator.Solution.SolutionPath == Path.GetFullPath(@"C:\projects\..\MyApp.sln")),
                @"D:\backup\dest.sds",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Not_Save_When_Relative_Path_Prompt_Cancelled()
        {
            _store.IsDirty.Returns(false);

            var document = new DependencyProjectDocument();
            document.DiagramGenerator.Solution.SolutionPath = @"..\MyApp.sln";

            _projectService.OpenAsync(@"C:\projects\source.sds", Arg.Any<CancellationToken>()).Returns(document);

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput(@"C:\projects\source.sds"));
            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(@"D:\backup\dest.sds"));
            _viewModel.RelativePathSaveAsInteraction.RegisterHandler(context => context.SetOutput(SaveAsRelativePathAction.Cancel));

            await _viewModel.NewFromExistingCommand.Execute();

            await _projectService.DidNotReceive().SaveAsync(Arg.Any<DependencyProjectDocument>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _store.DidNotReceive().OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Not_Prompt_When_Destination_Folder_Matches()
        {
            _store.IsDirty.Returns(false);

            var document = new DependencyProjectDocument();
            document.DiagramGenerator.Solution.SolutionPath = @"..\MyApp.sln";

            _projectService.OpenAsync(@"C:\projects\source.sds", Arg.Any<CancellationToken>()).Returns(document);

            var prompted = false;
            _viewModel.RelativePathSaveAsInteraction.RegisterHandler(context =>
            {
                prompted = true;
                context.SetOutput(SaveAsRelativePathAction.Cancel);
            });

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput(@"C:\projects\source.sds"));
            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput(@"C:\projects\dest.sds"));

            await _viewModel.NewFromExistingCommand.Execute();

            prompted.ShouldBeFalse();
            await _projectService.Received(1).SaveAsync(document, @"C:\projects\dest.sds", Arg.Any<CancellationToken>());
        }
    }

    public class OpenRecentProjectAsync : MainWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Open_File()
        {
            _store.IsDirty.Returns(false);

            await _viewModel.OpenRecentProjectCommand.Execute("recent.sds");

            await _store.Received(1).OpenAsync("recent.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Cancels()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("OldProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Cancel));

            await _viewModel.OpenRecentProjectCommand.Execute("recent.sds");

            await _store.DidNotReceive().OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Prompt_When_Dirty_And_User_Saves_First()
        {
            _store.IsDirty.Returns(true);

            var metadataEditor = Substitute.For<IProjectMetadataEditor>();
            metadataEditor.ProjectName.Returns(CreateTrackableValue("OldProject"));
            _store.MetadataEditor.Returns(metadataEditor);

            _viewModel.ConfirmDiscardInteraction.RegisterHandler(context => context.SetOutput(DiscardAction.Save));

            await _viewModel.OpenRecentProjectCommand.Execute("recent.sds");

            await _store.Received(1).SaveAsync(Arg.Any<CancellationToken>());
            await _store.Received(1).OpenAsync("recent.sds", Arg.Any<CancellationToken>());
        }
    }

    public class OpenRecentProjectError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public OpenRecentProjectError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_And_Remove_From_Recent()
        {
            _store.IsDirty.Returns(false);

            var exception = new InvalidOperationException("File not found");

            _store
                .OpenAsync("recent.sds", Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            // The failure is routed to the wired ThrownExceptions handler, which shows the error
            // dialog. The Execute task itself may fault or complete without a result depending on
            // the ReactiveUI version, so its outcome is observed and ignored rather than awaited.
            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.OpenRecentProjectCommand, "recent.sds");

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Open Recent Project failed");
            capturedError.Message.ShouldContain("moved or deleted");
            capturedError.Message.ShouldContain("File not found");

            _recentProjects.Received(1).Remove("recent.sds");
        }
    }

    public class OpenProjectError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public OpenProjectError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            _store.IsDirty.Returns(false);

            var exception = new InvalidOperationException("Access denied");

            _store
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput("test.sds"));

            // The failure is routed to the wired ThrownExceptions handler, which shows the error
            // dialog. The Execute task itself may fault or complete without a result depending on
            // the ReactiveUI version, so its outcome is observed and ignored rather than awaited.
            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.OpenProjectCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Open Project failed");
            capturedError.Message.ShouldContain("Access denied");
        }
    }

    public class NewProjectError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public NewProjectError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            _store.IsDirty.Returns(false);

            var exception = new InvalidOperationException("Save failed");

            _projectService
                .SaveAsync(Arg.Any<DependencyProjectDocument>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("new.sds"));

            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.NewProjectCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("New Project failed");
            capturedError.Message.ShouldContain("Save failed");
        }
    }

    public class NewFromExistingError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public NewFromExistingError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            _store.IsDirty.Returns(false);

            var exception = new InvalidOperationException("Open failed");

            _projectService
                .OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput("source.sds"));

            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.NewFromExistingCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("New from Existing failed");
            capturedError.Message.ShouldContain("Open failed");
        }
    }

    public class SaveError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public SaveError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            var exception = new InvalidOperationException("Save failed");

            _store
                .SaveAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.SaveCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Save failed");
            capturedError.Message.ShouldContain("Save failed");
        }
    }

    public class SaveAsError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public SaveAsError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            var exception = new InvalidOperationException("Save failed");

            _store
                .SaveAsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("test.sds"));

            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.SaveAsCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Save As failed");
            capturedError.Message.ShouldContain("Save failed");
        }
    }

    public class CloseProjectError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public CloseProjectError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            _store.IsDirty.Returns(false);

            var exception = new InvalidOperationException("Close failed");

            _store
                .When(store => store.Close())
                .Throw(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.CloseProjectCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Close Project failed");
            capturedError.Message.ShouldContain("Close failed");
        }
    }

    public class GenerateError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public GenerateError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            _store.IsDirty.Returns(false);

            var exception = new InvalidOperationException("Generation failed");

            _generationService
                .RunAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.GenerateCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Generation failed");
            capturedError.Message.ShouldContain("Generation failed");
        }
    }

    public class AnalyseError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public AnalyseError()
            : base(ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction))
        {
            _showErrorInteraction = interaction;
        }

        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            _store.IsDirty.Returns(false);

            var exception = new InvalidOperationException("Analysis failed");

            _analysisService
                .RunAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            _showErrorInteraction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(_viewModel.AnalyseCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Analysis failed");
            capturedError.Message.ShouldContain("Analysis failed");
        }
    }

    public class Navigation : MainWindowViewModelFixture
    {
        [Fact]
        public void Should_Contain_Project_Nav_Item()
        {
            _viewModel.NavigationItems
                .ShouldContain(item =>
                    item.DisplayName == "Project" &&
                    item.ViewModelType == typeof(ProjectViewModel));
        }

        [Fact]
        public void Should_Contain_Solution_Nav_Item()
        {
            _viewModel.NavigationItems
                .ShouldContain(item =>
                    item.DisplayName == "Solution" &&
                    item.ViewModelType == typeof(SolutionViewModel));
        }

        [Fact]
        public void Should_Contain_Export_Nav_Item()
        {
            _viewModel.NavigationItems
                .ShouldContain(item =>
                    item.DisplayName == "Export" &&
                    item.ViewModelType == typeof(ExportViewModel));
        }

        [Fact]
        public void Should_Contain_Diagrams_Nav_Item()
        {
            _viewModel.NavigationItems
                .ShouldContain(item =>
                    item.DisplayName == "Diagrams" &&
                    item.ViewModelType == typeof(DiagramsViewModel));
        }
    }

    public class ShowEmptyState : MainWindowViewModelFixture
    {
        [Fact]
        public void Should_Create_View_Via_Factory_And_Wire_NewProject_Interaction()
        {
            _store.IsDirty.Returns(false);

            var emptyStateView = Substitute.For<IViewFor<EmptyStateViewModel>>();
            var emptyStateVm = new EmptyStateViewModel(_recentProjects);

            emptyStateView.ViewModel.Returns(emptyStateVm);
            _viewFactory.CreateViewFor<EmptyStateViewModel>().Returns(emptyStateView);

            _viewModel.ShowEmptyState();

            _viewFactory.Received(1).CreateViewFor<EmptyStateViewModel>();
            _viewModel.CurrentPage.ShouldBe(emptyStateView);
            _viewModel.SelectedNavigationItem.ShouldBeNull();
        }

        [Fact]
        public async Task Should_Wire_Interactions_So_NewProject_Triggers_Shell_Command()
        {
            _store.IsDirty.Returns(false);

            var emptyStateView = Substitute.For<IViewFor<EmptyStateViewModel>>();
            var emptyStateVm = new EmptyStateViewModel(_recentProjects);

            emptyStateView.ViewModel.Returns(emptyStateVm);
            _viewFactory.CreateViewFor<EmptyStateViewModel>().Returns(emptyStateView);

            var document = new DependencyProjectDocument();
            _projectService.CreateFromDefaults().Returns(document);
            _viewModel.SaveFileInteraction.RegisterHandler(context => context.SetOutput("new.sds"));

            _viewModel.ShowEmptyState();

            // Execute the empty-state's NewProjectCommand.
            // The handler registered in ShowEmptyState should delegate to the shell's NewProjectCommand.
            await emptyStateVm.NewProjectCommand.Execute();

            await _projectService.Received(1).SaveAsync(document, "new.sds", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Wire_Interactions_So_OpenRecentProject_Triggers_Shell_Command()
        {
            _store.IsDirty.Returns(false);

            var emptyStateView = Substitute.For<IViewFor<EmptyStateViewModel>>();
            var emptyStateVm = new EmptyStateViewModel(_recentProjects);

            emptyStateView.ViewModel.Returns(emptyStateVm);
            _viewFactory.CreateViewFor<EmptyStateViewModel>().Returns(emptyStateView);

            _viewModel.ShowEmptyState();

            await emptyStateVm.OpenRecentProjectCommand.Execute("recent.sds");

            await _store.Received(1).OpenAsync("recent.sds", Arg.Any<CancellationToken>());
        }
    }

    public class AnalyseCommand : MainWindowViewModelFixture
    {
        [Fact]
        public void Should_Be_Disabled_When_No_Document()
        {
            // The base fixture sets _store.HasDocument.Returns(true) AFTER construction.
            // For this test, create a fresh ViewModel where HasDocument starts as false.
            var store = Substitute.For<IProjectDocumentStore>();
            store.HasDocument.Returns(false);

            var vm = new MainWindowViewModel(
                store, _projectService, _recentProjects, _errorDialog, _viewFactory,
                _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

            var canExecute = vm.AnalyseCommand.CanExecute.FirstAsync().Wait();

            canExecute.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_Enabled_When_Document_Loaded()
        {
            // Create a ViewModel with HasDocument=true from the start so
            // WhenAnyValue picks up the correct initial value.
            var store = Substitute.For<IProjectDocumentStore>();
            store.HasDocument.Returns(true);

            var vm = new MainWindowViewModel(
                store, _projectService, _recentProjects, _errorDialog, _viewFactory,
                _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

            var canExecute = vm.AnalyseCommand.CanExecute.FirstAsync().Wait();

            canExecute.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Invoke_AnalysisService_On_Execute()
        {
            _analysisService
                .RunAsync(Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            await _viewModel.AnalyseCommand.Execute();

            await _analysisService.Received(1).RunAsync(Arg.Any<CancellationToken>());
        }
    }

    public class IsOperationRunningState : MainWindowViewModelFixture
    {
        [Fact]
        public void Should_Be_False_By_Default()
        {
            // IsOperationRunning is driven by a subscription in WireRunMenuGating
            // (called from OnActivated), which is not triggered in unit tests.
            // Only the default value is verifiable.
            _viewModel.IsOperationRunning.ShouldBeFalse();
        }
    }

    public class CanCloseState : MainWindowViewModelFixture
    {
        [Fact]
        public void Should_Be_True_By_Default()
        {
            // CanClose is driven by a subscription in WireRunMenuGating
            // (called from OnActivated), which is not triggered in unit tests.
            // Only the default value is verifiable.
            _viewModel.CanClose.ShouldBeTrue();
        }
    }

    public class GenerateCommand : MainWindowViewModelFixture
    {
        [Fact]
        public void Should_Be_Disabled_When_No_Document()
        {
            var store = Substitute.For<IProjectDocumentStore>();
            store.HasDocument.Returns(false);

            var vm = new MainWindowViewModel(
                store, _projectService, _recentProjects, _errorDialog, _viewFactory,
                _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

            var canExecute = vm.GenerateCommand.CanExecute.FirstAsync().Wait();

            canExecute.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_Enabled_When_Document_Loaded()
        {
            var store = Substitute.For<IProjectDocumentStore>();
            store.HasDocument.Returns(true);

            var vm = new MainWindowViewModel(
                store, _projectService, _recentProjects, _errorDialog, _viewFactory,
                _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

            var canExecute = vm.GenerateCommand.CanExecute.FirstAsync().Wait();

            canExecute.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Invoke_GenerationService_On_Execute()
        {
            var completion = new TaskCompletionSource();

            _generationService
                .RunAsync(Arg.Any<CancellationToken>())
                .Returns(completion.Task);

            // Execute and wait briefly for the command to start
            var executeTask = _viewModel.GenerateCommand.Execute();

            // Complete the generation
            completion.SetResult();

            await executeTask;

            await _generationService.Received(1).RunAsync(Arg.Any<CancellationToken>());
        }
    }

    public class CancelOperation : MainWindowViewModelFixture
    {
        [Fact]
        public void Should_Be_True_By_Default()
        {
            // CanCancel, OperationName, and the cancel execution path go through
            // private methods (CancelOperationAsync, AnalyseAsync/GenerateAsync)
            // wired in OnActivated — the ReactiveUI activation infrastructure is
            // not triggered in unit tests. These tests verify the public observable
            // contract: initial state and post-operation cleanup.
            _viewModel.CanCancel.ShouldBeTrue();
            _viewModel.OperationName.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task Should_Be_True_During_Analyse()
        {
            var completion = new TaskCompletionSource();

            _analysisService
                .RunAsync(Arg.Any<CancellationToken>())
                .Returns(completion.Task);

            var executingTask = _viewModel.AnalyseCommand.Execute();

            // CanCancel is set inside the async method body (AnalyseAsync) before
            // it awaits RunAsync, so it is already set at this point.
            _viewModel.CanCancel.ShouldBeTrue();

            completion.SetResult();
            await executingTask;
        }

        [Fact]
        public async Task OperationName_Should_Reset_To_Empty_After_Operation_Completes()
        {
            var completion = new TaskCompletionSource();

            _analysisService
                .RunAsync(Arg.Any<CancellationToken>())
                .Returns(completion.Task);

            var executingTask = _viewModel.AnalyseCommand.Execute();
            completion.SetResult();
            await executingTask;

            _viewModel.OperationName.ShouldBe(string.Empty);
        }
    }

    public class HasRecentProjects : MainWindowViewModelFixture
    {
        [Fact]
        public void Should_Start_False_And_Become_True_When_Project_Is_Added()
        {
            // Use the real RecentProjectsStore so PropertyChanged notifications
            // flow through to the ViewModel's WhenAnyValue subscription.
            var service = Substitute.For<IRecentProjectsService>();
            service.GetRecent().Returns([]);

            var realStore = new RecentProjectsStore(service, Substitute.For<ILogger<RecentProjectsStore>>());

            var vm = new MainWindowViewModel(
                _store, _projectService, realStore, _errorDialog, _viewFactory,
                _analysisService, _generationService,
                Substitute.For<ILogger<MainWindowViewModel>>());

            // Should start false — clean slate, no state.json yet
            vm.HasRecentProjects.ShouldBeFalse();

            // Simulate the service persisting the entry, then the store adding it
            service.GetRecent().Returns([
                new RecentProjectEntry(@"C:\test.sds", "test", true)
            ]);

            realStore.Add(@"C:\test.sds");

            vm.HasRecentProjects.ShouldBeTrue();
        }
    }

    /// <summary>
    /// Verifies that page selection is derived from document state via the store's
    /// <see cref="IProjectDocumentStore.DocumentEpoch"/> signal, rather than commanded
    /// imperatively from each operation. Uses a real <see cref="ProjectDocumentStore"/>
    /// (so ReactiveUI change notifications reach the shell's subscription) and an
    /// activated ViewModel.
    /// </summary>
    public class DocumentNavigation : MainWindowViewModelFixture
    {
        private readonly ProjectDocumentStore _realStore;
        private readonly MainWindowViewModel _shellViewModel;

        public DocumentNavigation()
        {
            _realStore = new ProjectDocumentStore(
                _projectService, _recentProjects, CreateEditorFactory(), Substitute.For<ILogger<ProjectDocumentStore>>());

            _shellViewModel = new MainWindowViewModel(
                _realStore, _projectService, _recentProjects, _errorDialog, _viewFactory,
                _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

            // Activate the ViewModel so the OnActivated subscriptions (including the
            // DocumentEpoch navigation pipeline) become live. Each test gets a fresh
            // instance, so the activation is intentionally never deactivated.
            _ = _shellViewModel.Activator.Activate();
        }

        [Fact]
        public void Should_Show_Empty_State_On_Activation_When_No_Document()
        {
            _shellViewModel.SelectedNavigationItem.ShouldBeNull();
            _viewFactory.Received(1).CreateViewFor<EmptyStateViewModel>();
        }

        [Fact]
        public async Task Should_Navigate_To_Project_When_Document_Opened()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument());

            await _realStore.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _shellViewModel.SelectedNavigationItem!.ViewModelType.ShouldBe(typeof(ProjectViewModel));
        }

        [Fact]
        public async Task Should_Navigate_To_Project_When_Second_Document_Opened_While_One_Is_Open()
        {
            _projectService
                .OpenAsync("one.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument());

            await _realStore.OpenAsync("one.sds", TestContext.Current.CancellationToken);

            // Move to another page to prove the open genuinely triggers navigation.
            _shellViewModel.SelectedNavigationItem = _shellViewModel.NavigationItems.Single(item => item.ViewModelType == typeof(ExportViewModel));

            _projectService
                .OpenAsync("two.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument());

            await _realStore.OpenAsync("two.sds", TestContext.Current.CancellationToken);

            _shellViewModel.SelectedNavigationItem!.ViewModelType.ShouldBe(typeof(ProjectViewModel));
        }

        [Fact]
        public async Task Should_Show_Empty_State_When_Document_Closed()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument());

            await _realStore.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _realStore.Close();

            _shellViewModel.SelectedNavigationItem.ShouldBeNull();
            _viewFactory.Received(2).CreateViewFor<EmptyStateViewModel>();
        }

        [Fact]
        public async Task Should_Not_Navigate_When_Save_As()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument());

            await _realStore.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            // Move to another page — Save As must leave the current page untouched.
            _shellViewModel.SelectedNavigationItem = _shellViewModel.NavigationItems.Single(item => item.ViewModelType == typeof(ExportViewModel));

            await _realStore.SaveAsAsync("copy.sds", TestContext.Current.CancellationToken);

            _shellViewModel.SelectedNavigationItem!.ViewModelType.ShouldBe(typeof(ExportViewModel));
        }

        private static DependencyProjectDocument CreateDocument()
        {
            return new DependencyProjectDocument
            {
                Metadata = new DependencyProjectMetadata
                {
                    ProjectName = "Test",
                    Description = "Desc"
                }
            };
        }
    }

    private static IStudioEditorFactory CreateEditorFactory()
    {
        var factory = Substitute.For<IStudioEditorFactory>();

        // The DocumentNavigation tests drive a real ProjectDocumentStore so ReactiveUI
        // change notifications flow through to the shell's DocumentEpoch subscription.
        // The store therefore needs the real editor wrappers (substitutes would be no-ops).
        factory.CreateEditor<IProjectMetadataEditor>().Returns(new ProjectMetadataEditor(Substitute.For<ILogger<ProjectMetadataEditor>>()));
        factory.CreateEditor<ISolutionOptionsEditor>().Returns(new SolutionOptionsEditor(Substitute.For<ILogger<SolutionOptionsEditor>>()));
        factory.CreateEditor<IExportOptionsEditor>().Returns(new ExportOptionsEditor(Substitute.For<ILogger<ExportOptionsEditor>>()));
        factory.CreateEditor<IDiagramOptionsEditor>().Returns(new DiagramOptionsEditor(Substitute.For<ILogger<DiagramOptionsEditor>>()));
        factory.CreateEditor<IPreGenerationConfigEditor>().Returns(new PreGenerationConfigEditor(Substitute.For<ILogger<PreGenerationConfigEditor>>()));
        factory.CreateEditor<IRestoreSolutionEditor>().Returns(new RestoreSolutionEditor(Substitute.For<ILogger<RestoreSolutionEditor>>()));
        factory.CreateEditor<IPostGenerationConfigEditor>().Returns(new PostGenerationConfigEditor(Substitute.For<ILogger<PostGenerationConfigEditor>>()));

        return factory;
    }

    private static IViewFor<T> CreateMockView<T>() where T : class
    {
        return Substitute.For<IViewFor<T>>();
    }

    private static TrackableValue<string> CreateTrackableValue(string value)
    {
        var trackable = new TrackableValue<string>();

        trackable.SetOriginalValue(value);

        return trackable;
    }
}
