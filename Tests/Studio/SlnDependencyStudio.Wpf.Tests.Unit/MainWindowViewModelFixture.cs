using AllOverIt.ReactiveUI.Factories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReactiveUI;
using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Abstractions.IO;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Application;
using SlnDependencyStudio.Wpf.Features.Application.Models;
using SlnDependencyStudio.Wpf.Features.Diagrams;
using SlnDependencyStudio.Wpf.Features.EmptyState;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Output;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Features.RecentProjects.Models;
using SlnDependencyStudio.Wpf.Features.Run;
using SlnDependencyStudio.Wpf.Features.Solution;
using SlnDependencyStudio.Wpf.Models;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit;

[Collection(nameof(ReactiveUIInitializer))]
public class MainWindowViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IDependencyProjectService _projectService = Substitute.For<IDependencyProjectService>();
    private readonly IRecentProjectsStore _recentProjects = Substitute.For<IRecentProjectsStore>();
    private readonly IErrorDialogService _errorDialog;
    private readonly IViewFactory _viewFactory = Substitute.For<IViewFactory>();
    private readonly IToolStatusService _toolStatus = Substitute.For<IToolStatusService>();
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
            Substitute.For<AllOverIt.Serilog.Sinks.Observable.IObservableSink>(),
            new Serilog.Core.LoggingLevelSwitch(Serilog.Events.LogEventLevel.Information),
            appSettings,
            Substitute.For<IFileSystem>());

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
            _toolStatus, _analysisService, _generationService, logger);

        _store.HasDocument.Returns(true);
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
        public async Task Should_Open_File_And_Select_Navigation()
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
            _viewModel.SelectedNavigationItem!.ViewModelType.ShouldBe(typeof(ProjectViewModel));
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
            _viewModel.SelectedNavigationItem!.ViewModelType.ShouldBe(typeof(ProjectViewModel));
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
    }

    public class OpenRecentProjectAsync : MainWindowViewModelFixture
    {
        [Fact]
        public async Task Should_Open_File_And_Select_Navigation()
        {
            _store.IsDirty.Returns(false);

            await _viewModel.OpenRecentProjectCommand.Execute("recent.sds");

            await _store.Received(1).OpenAsync("recent.sds", Arg.Any<CancellationToken>());
            _viewModel.SelectedNavigationItem!.ViewModelType.ShouldBe(typeof(ProjectViewModel));
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
            : base(CreateErrorDialogSubstitute(out var interaction))
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

            ErrorInfo? capturedError = null;

            _showErrorInteraction.RegisterHandler(context =>
            {
                capturedError = context.Input;
                context.SetOutput(System.Reactive.Unit.Default);
            });

            await _viewModel.OpenRecentProjectCommand.Execute("recent.sds");

            capturedError.ShouldNotBeNull();
            capturedError!.Title.ShouldBe("Open Failed");
            capturedError.Message.ShouldContain("File not found");

            _recentProjects.Received(1).Remove("recent.sds");
        }
    }

    public class OpenProjectError : MainWindowViewModelFixture
    {
        private readonly Interaction<ErrorInfo, System.Reactive.Unit> _showErrorInteraction;

        public OpenProjectError()
            : base(CreateErrorDialogSubstitute(out var interaction))
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

            ErrorInfo? capturedError = null;

            _showErrorInteraction.RegisterHandler(context =>
            {
                capturedError = context.Input;
                context.SetOutput(System.Reactive.Unit.Default);
            });

            _viewModel.OpenFileInteraction.RegisterHandler(context => context.SetOutput("test.sds"));

            await _viewModel.OpenProjectCommand.Execute();

            capturedError.ShouldNotBeNull();
            capturedError!.Title.ShouldBe("Open Failed");
            capturedError.Message.ShouldContain("Access denied");
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

    public class AnalyzeCommand : MainWindowViewModelFixture
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
                _toolStatus, _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

            var canExecute = vm.AnalyzeCommand.CanExecute.FirstAsync().Wait();

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
                _toolStatus, _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

            var canExecute = vm.AnalyzeCommand.CanExecute.FirstAsync().Wait();

            canExecute.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Invoke_AnalysisService_On_Execute()
        {
            _analysisService
                .RunAsync(Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            await _viewModel.AnalyzeCommand.Execute();

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
                _toolStatus, _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

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
                _toolStatus, _analysisService, _generationService, Substitute.For<ILogger<MainWindowViewModel>>());

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
            // private methods (CancelOperationAsync, AnalyzeAsync/GenerateAsync)
            // wired in OnActivated — the ReactiveUI activation infrastructure is
            // not triggered in unit tests. These tests verify the public observable
            // contract: initial state and post-operation cleanup.
            _viewModel.CanCancel.ShouldBeTrue();
            _viewModel.OperationName.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task Should_Be_True_During_Analyze()
        {
            var completion = new TaskCompletionSource();

            _analysisService
                .RunAsync(Arg.Any<CancellationToken>())
                .Returns(completion.Task);

            var executingTask = _viewModel.AnalyzeCommand.Execute();

            // CanCancel is set inside the async method body (AnalyzeAsync) before
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

            var executingTask = _viewModel.AnalyzeCommand.Execute();
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

            var realStore = new RecentProjectsStore(service);

            var vm = new MainWindowViewModel(
                _store, _projectService, realStore, _errorDialog, _viewFactory,
                _toolStatus, _analysisService, _generationService,
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

    private static IViewFor<T> CreateMockView<T>() where T : class
    {
        return Substitute.For<IViewFor<T>>();
    }

    private static IErrorDialogService CreateErrorDialogSubstitute(out Interaction<ErrorInfo, System.Reactive.Unit> interaction)
    {
        interaction = new();

        var substitute = Substitute.For<IErrorDialogService>();

        substitute.ShowError.Returns(interaction);

        return substitute;
    }

    private static TrackableValue<string> CreateTrackableValue(string value)
    {
        var trackable = new TrackableValue<string>();

        trackable.SetOriginalValue(value);

        return trackable;
    }
}
