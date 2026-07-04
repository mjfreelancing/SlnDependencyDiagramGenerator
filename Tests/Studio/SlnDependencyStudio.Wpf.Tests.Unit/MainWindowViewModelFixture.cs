using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Models;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit;

[Collection(nameof(ReactiveUIInitializer))]
public class MainWindowViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IDependencyProjectService _projectService = Substitute.For<IDependencyProjectService>();
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelFixture()
    {
        var logger = Substitute.For<ILogger<MainWindowViewModel>>();

        _viewModel = new MainWindowViewModel(_store, _projectService, null!, logger);

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
    }

    private static TrackableValue<string> CreateTrackableValue(string value)
    {
        var trackable = new TrackableValue<string>();

        trackable.SetOriginalValue(value);

        return trackable;
    }
}
