using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Pipeline;

[Collection(nameof(ReactiveUIInitializer))]
public class PipelineViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly TrackableValue<bool> _enabled = new();
    private readonly TrackableValue<string> _command = new();
    private readonly TrackableValue<string> _arguments = new();
    private readonly TrackableValue<string> _workingDirectory = new();
    private readonly TrackableValue<bool> _continueOnFailure = new();
    private readonly IPreGenerationConfigEditor _editor = Substitute.For<IPreGenerationConfigEditor>();
    private readonly PipelineViewModel _viewModel;

    public PipelineViewModelFixture()
    {
        _enabled.SetOriginalValue(false);
        _command.SetOriginalValue(string.Empty);
        _arguments.SetOriginalValue(string.Empty);
        _workingDirectory.SetOriginalValue(string.Empty);
        _continueOnFailure.SetOriginalValue(false);

        _editor.Enabled.Returns(_enabled);
        _editor.Command.Returns(_command);
        _editor.Arguments.Returns(_arguments);
        _editor.WorkingDirectory.Returns(_workingDirectory);
        _editor.ContinueOnFailure.Returns(_continueOnFailure);
        _store.PreGenerationEditor.Returns(_editor);

        _viewModel = new PipelineViewModel(_store);
    }

    public class Construction : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Expose_Enabled_From_Store()
        {
            _viewModel.Enabled.ShouldBeSameAs(_enabled);
        }

        [Fact]
        public void Should_Expose_Command_From_Store()
        {
            _viewModel.Command.ShouldBeSameAs(_command);
        }

        [Fact]
        public void Should_Expose_Arguments_From_Store()
        {
            _viewModel.Arguments.ShouldBeSameAs(_arguments);
        }

        [Fact]
        public void Should_Expose_WorkingDirectory_From_Store()
        {
            _viewModel.WorkingDirectory.ShouldBeSameAs(_workingDirectory);
        }

        [Fact]
        public void Should_Expose_ContinueOnFailure_From_Store()
        {
            _viewModel.ContinueOnFailure.ShouldBeSameAs(_continueOnFailure);
        }

        [Fact]
        public void Should_Seed_UseRelativePath_Defaults()
        {
            _viewModel.UseRelativePathForCommand.Value.ShouldBeTrue();
            _viewModel.UseRelativePathForWorkingDirectory.Value.ShouldBeTrue();
        }

        [Fact]
        public void Should_Have_ValidationContext()
        {
            _viewModel.ValidationContext.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Have_BrowseCommands()
        {
            _viewModel.BrowseCommandCommand.ShouldNotBeNull();
            _viewModel.BrowseWorkingDirectoryCommand.ShouldNotBeNull();
        }
    }

    public class BrowseCommands : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Set_Command_Path_When_Browsed()
        {
            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\tools\build.cmd");
            });

            _viewModel.BrowseCommandCommand.Execute().Subscribe();

            _command.Value.ShouldBe(@"C:\tools\build.cmd");
        }

        [Fact]
        public void Should_Not_Change_Command_When_Browse_Cancelled()
        {
            _command.Value = @"C:\old\run.bat";

            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(null);
            });

            _viewModel.BrowseCommandCommand.Execute().Subscribe();

            _command.Value.ShouldBe(@"C:\old\run.bat");
        }

        [Fact]
        public void Should_Set_WorkingDirectory_When_Browsed()
        {
            _viewModel.BrowseWorkingDirectoryInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\work");
            });

            _viewModel.BrowseWorkingDirectoryCommand.Execute().Subscribe();

            _workingDirectory.Value.ShouldBe(@"C:\work");
        }
    }

    public class Validation : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Pass_When_Enabled_False_And_Command_Empty()
        {
            _enabled.Value = false;
            _command.Value = string.Empty;

            _viewModel.ValidationContext.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_Fail_When_Enabled_True_And_Command_Empty()
        {
            _command.Value = string.Empty;
            _enabled.Value = true;

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Pass_When_Enabled_True_And_Command_Not_Empty()
        {
            var enabled = new TrackableValue<bool>();
            enabled.SetOriginalValue(true);
            var command = new TrackableValue<string>();
            command.SetOriginalValue("build.cmd");

            _editor.Enabled.Returns(enabled);
            _editor.Command.Returns(command);

            var viewModel = new PipelineViewModel(_store);

            viewModel.ValidationContext.IsValid.ShouldBeTrue();
        }
    }

    public class PreGenError : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Be_Null_When_Toggle_Off()
        {
            _enabled.Value = false;
            _command.Value = string.Empty;

            _viewModel.PreGenError.ShouldBeNull();
        }

        [Fact]
        public void Should_Be_Null_When_Command_Not_Empty()
        {
            _enabled.Value = true;
            _command.Value = "build.cmd";

            _viewModel.PreGenError.ShouldBeNull();
        }

        [Fact]
        public void Should_Contain_Error_When_Toggle_On_And_Command_Empty()
        {
            _enabled.Value = true;
            _command.Value = string.Empty;

            _viewModel.PreGenError.ShouldContain("Command must not be empty");
        }

        [Fact]
        public void Should_Clear_When_Command_Filled()
        {
            _enabled.Value = true;
            _command.Value = string.Empty;

            _viewModel.PreGenError.ShouldNotBeNull();

            _command.Value = "build.cmd";

            _viewModel.PreGenError.ShouldBeNull();
        }

        [Fact]
        public void Should_Clear_When_Toggle_Turned_Off()
        {
            _enabled.Value = true;
            _command.Value = string.Empty;

            _viewModel.PreGenError.ShouldNotBeNull();

            _enabled.Value = false;

            _viewModel.PreGenError.ShouldBeNull();
        }
    }
}
