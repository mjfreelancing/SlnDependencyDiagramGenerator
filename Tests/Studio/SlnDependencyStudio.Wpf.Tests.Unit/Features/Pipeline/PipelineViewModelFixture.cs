using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Pipeline;

[Collection(nameof(ReactiveUIInitializer))]
public class PipelineViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IToolStatusService _toolStatus = Substitute.For<IToolStatusService>();
    private readonly TrackableValue<bool> _enabled = new();
    private readonly TrackableValue<string> _command = new();
    private readonly TrackableValue<string> _arguments = new();
    private readonly TrackableValue<string> _workingDirectory = new();
    private readonly TrackableValue<bool> _continueOnFailure = new();
    private readonly IPreGenerationConfigEditor _editor = Substitute.For<IPreGenerationConfigEditor>();
    private readonly BehaviorSubject<IReadOnlyList<ToolStatusEntry>> _toolStatusSubject = new(Array.Empty<ToolStatusEntry>());
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

        _toolStatus.ToolStatuses.Returns(_toolStatusSubject.AsObservable());

        _viewModel = new PipelineViewModel(_store, _toolStatus);
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
        public void Should_Set_Command_To_Filename_When_Browsed()
        {
            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\tools\build.cmd");
            });

            _viewModel.BrowseCommandCommand.Execute().Subscribe();

            _command.Value.ShouldBe("build.cmd");
        }

        [Fact]
        public void Should_Populate_WorkingDirectory_When_Empty_And_Command_Browsed()
        {
            _workingDirectory.Value = string.Empty;

            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\tools\build.cmd");
            });

            _viewModel.BrowseCommandCommand.Execute().Subscribe();

            _workingDirectory.Value.ShouldBe(@"C:\tools");
        }

        [Fact]
        public void Should_Not_Overwrite_WorkingDirectory_When_Command_Browsed()
        {
            _workingDirectory.Value = @"C:\existing";

            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\tools\build.cmd");
            });

            _viewModel.BrowseCommandCommand.Execute().Subscribe();

            _workingDirectory.Value.ShouldBe(@"C:\existing");
        }

        [Fact]
        public void Should_Not_Change_Command_When_Browse_Cancelled()
        {
            _command.Value = "run.bat";

            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(null);
            });

            _viewModel.BrowseCommandCommand.Execute().Subscribe();

            _command.Value.ShouldBe("run.bat");
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

    public class RelativePathToggle : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Convert_To_Relative_When_Toggled_On()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            // Start with an absolute path, toggle off
            _viewModel.UseRelativePathForWorkingDirectory.Value = false;
            _workingDirectory.Value = @"C:\projects\src";

            // Toggle on — should convert to relative
            _viewModel.UseRelativePathForWorkingDirectory.Value = true;

            _workingDirectory.Value.ShouldBe("src");
        }

        [Fact]
        public void Should_Convert_To_Absolute_When_Toggled_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            // Start with a relative path, toggle on
            _viewModel.UseRelativePathForWorkingDirectory.Value = true;
            _workingDirectory.Value = "src";

            // Toggle off — should convert to absolute
            _viewModel.UseRelativePathForWorkingDirectory.Value = false;

            _workingDirectory.Value.ShouldBe(@"C:\projects\src");
        }

        [Fact]
        public void Should_Not_Change_Already_Relative_Path_When_Toggled_On()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _viewModel.UseRelativePathForWorkingDirectory.Value = true;
            _workingDirectory.Value = @"..\..\..\AllOverIt";

            // Toggle again — should not be re-resolved against process CWD
            _viewModel.UseRelativePathForWorkingDirectory.Value = true;

            _workingDirectory.Value.ShouldBe(@"..\..\..\AllOverIt");
        }

        [Fact]
        public void Should_Not_Change_Already_Absolute_Path_When_Toggled_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _viewModel.UseRelativePathForWorkingDirectory.Value = false;
            _workingDirectory.Value = @"C:\tools";

            // Toggle again — should not be re-converted
            _viewModel.UseRelativePathForWorkingDirectory.Value = false;

            _workingDirectory.Value.ShouldBe(@"C:\tools");
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

            var viewModel = new PipelineViewModel(_store, _toolStatus);

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

    public class ToolStatus : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Have_Empty_Entries_Initially()
        {
            _viewModel.ToolStatusEntries.ShouldBeEmpty();
        }

        [Fact]
        public void Should_Populate_Entries_When_Status_Arrives()
        {
            var d2Entry = new ToolStatusEntry { ToolName = "d2", IsAvailable = true, ResolvedPath = @"C:\tools\d2.exe" };
            var mmdcEntry = new ToolStatusEntry { ToolName = "mmdc", IsAvailable = false };

            _toolStatusSubject.OnNext([d2Entry, mmdcEntry]);

            _viewModel.ToolStatusEntries.Count.ShouldBe(2);
            _viewModel.ToolStatusEntries[0].ToolName.ShouldBe("d2");
            _viewModel.ToolStatusEntries[0].IsAvailable.ShouldBeTrue();
            _viewModel.ToolStatusEntries[1].ToolName.ShouldBe("mmdc");
            _viewModel.ToolStatusEntries[1].IsAvailable.ShouldBeFalse();
        }

        [Fact]
        public void Should_Replace_Entries_On_Subsequent_Update()
        {
            _toolStatusSubject.OnNext([new ToolStatusEntry { ToolName = "d2", IsAvailable = false }]);

            _viewModel.ToolStatusEntries.Count.ShouldBe(1);

            _toolStatusSubject.OnNext(
            [
                new ToolStatusEntry { ToolName = "d2", IsAvailable = true },
                new ToolStatusEntry { ToolName = "mmdc", IsAvailable = true }
            ]);

            _viewModel.ToolStatusEntries.Count.ShouldBe(2);
            _viewModel.ToolStatusEntries[0].IsAvailable.ShouldBeTrue();
            _viewModel.ToolStatusEntries[1].IsAvailable.ShouldBeTrue();
        }
    }

    public class RescanCommand : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Have_RescanToolsCommand()
        {
            _viewModel.RescanToolsCommand.ShouldNotBeNull();
        }

        [Fact]
        public async Task Should_Call_RescanAsync_On_Execute()
        {
            await _viewModel.RescanToolsCommand.Execute();

            await _toolStatus.Received(1).RescanAsync(Arg.Any<CancellationToken>());
        }
    }
}
