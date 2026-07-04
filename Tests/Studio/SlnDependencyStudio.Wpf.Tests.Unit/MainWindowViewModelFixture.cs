using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Models;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit;

[Collection(nameof(ReactiveUIInitializer))]
public class MainWindowViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelFixture()
    {
        var logger = Substitute.For<ILogger<MainWindowViewModel>>();

        _viewModel = new MainWindowViewModel(_store, null!, logger);

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
                context.Input.ShouldBe("SlnDependencyStudio project files (*.sds)|*.sds|All files (*.*)|*.*");
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

    private static TrackableValue<string> CreateTrackableValue(string value)
    {
        var trackable = new TrackableValue<string>();

        trackable.SetOriginalValue(value);

        return trackable;
    }
}
