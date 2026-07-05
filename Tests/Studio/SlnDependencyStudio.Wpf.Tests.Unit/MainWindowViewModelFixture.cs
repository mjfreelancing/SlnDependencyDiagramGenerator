using AllOverIt.ReactiveUI.Factories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ReactiveUI;
using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.EmptyState;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.RecentProjects;
using SlnDependencyStudio.Wpf.Models;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit;

[Collection(nameof(ReactiveUIInitializer))]
public class MainWindowViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IDependencyProjectService _projectService = Substitute.For<IDependencyProjectService>();
    private readonly IRecentProjectsService _recentProjects = Substitute.For<IRecentProjectsService>();
    private readonly IErrorDialogService _errorDialog;
    private readonly IViewFactory _viewFactory = Substitute.For<IViewFactory>();
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelFixture()
        : this(Substitute.For<IErrorDialogService>())
    {
    }

    protected MainWindowViewModelFixture(IErrorDialogService errorDialog)
    {
        _errorDialog = errorDialog;

        var logger = Substitute.For<ILogger<MainWindowViewModel>>();

        _viewModel = new MainWindowViewModel(_store, _projectService, _recentProjects, _errorDialog, _viewFactory, logger);

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
