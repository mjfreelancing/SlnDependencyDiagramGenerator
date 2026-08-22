using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.ErrorDialog;
using SlnDependencyStudio.Wpf.Features.Pipeline;
using SlnDependencyStudio.Wpf.Features.Pipeline.Models;
using SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.PreGeneration;
using SlnDependencyStudio.Wpf.Features.Pipeline.RestoreSolution;
using SlnDependencyStudio.Wpf.Features.Pipeline.Services;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;

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
    private readonly TrackableValue<bool> _useRelativePath = new();
    private readonly IPreGenerationConfigEditor _editor = Substitute.For<IPreGenerationConfigEditor>();
    private readonly TrackableValue<bool> _restoreSolution = new();
    private readonly IRestoreSolutionEditor _restoreSolutionEditor = Substitute.For<IRestoreSolutionEditor>();
    private readonly TrackableValue<bool> _postGenEnabled = new();
    private readonly TrackableValue<string> _postGenCommand = new();
    private readonly TrackableValue<string> _postGenArguments = new();
    private readonly TrackableValue<string> _postGenWorkingDirectory = new();
    private readonly TrackableValue<bool> _postGenUseRelativePath = new();
    private readonly IPostGenerationConfigEditor _postGenEditor = Substitute.For<IPostGenerationConfigEditor>();
    private readonly BehaviorSubject<IReadOnlyList<ToolStatusEntry>> _toolStatusSubject = new(Array.Empty<ToolStatusEntry>());
    private readonly PipelineViewModel _viewModel;

    public PipelineViewModelFixture()
    {
        _enabled.SetOriginalValue(false);
        _command.SetOriginalValue(string.Empty);
        _arguments.SetOriginalValue(string.Empty);
        _workingDirectory.SetOriginalValue(string.Empty);
        _continueOnFailure.SetOriginalValue(false);

        _restoreSolution.SetOriginalValue(true);
        _postGenEnabled.SetOriginalValue(false);
        _postGenCommand.SetOriginalValue(string.Empty);
        _postGenArguments.SetOriginalValue(string.Empty);
        _postGenWorkingDirectory.SetOriginalValue(string.Empty);
        _useRelativePath.SetOriginalValue(true);
        _postGenUseRelativePath.SetOriginalValue(true);

        _editor.Enabled.Returns(_enabled);
        _editor.Command.Returns(_command);
        _editor.Arguments.Returns(_arguments);
        _editor.WorkingDirectory.Returns(_workingDirectory);
        _editor.UseRelativePath.Returns(_useRelativePath);
        _editor.ContinueOnFailure.Returns(_continueOnFailure);
        _store.PreGenerationEditor.Returns(_editor);

        _restoreSolutionEditor.RestoreSolution.Returns(_restoreSolution);
        _store.RestoreSolutionEditor.Returns(_restoreSolutionEditor);

        _postGenEditor.Enabled.Returns(_postGenEnabled);
        _postGenEditor.Command.Returns(_postGenCommand);
        _postGenEditor.Arguments.Returns(_postGenArguments);
        _postGenEditor.WorkingDirectory.Returns(_postGenWorkingDirectory);
        _postGenEditor.UseRelativePath.Returns(_postGenUseRelativePath);
        _store.PostGenerationEditor.Returns(_postGenEditor);

        _toolStatus.ToolStatuses.Returns(_toolStatusSubject.AsObservable());

        _viewModel = new PipelineViewModel(_store, _toolStatus, Substitute.For<IErrorDialogService>(), Substitute.For<ILogger<PipelineViewModel>>());
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
            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value.ShouldBeTrue();
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

        [Fact]
        public void Should_Expose_RestoreSolution_From_Store()
        {
            _viewModel.RestoreSolution.ShouldBeSameAs(_restoreSolution);
        }

        [Fact]
        public void Should_Expose_PostGenEnabled_From_Store()
        {
            _viewModel.PostGenEnabled.ShouldBeSameAs(_postGenEnabled);
        }

        [Fact]
        public void Should_Expose_PostGenCommand_From_Store()
        {
            _viewModel.PostGenCommand.ShouldBeSameAs(_postGenCommand);
        }

        [Fact]
        public void Should_Expose_PostGenArguments_From_Store()
        {
            _viewModel.PostGenArguments.ShouldBeSameAs(_postGenArguments);
        }

        [Fact]
        public void Should_Expose_PostGenWorkingDirectory_From_Store()
        {
            _viewModel.PostGenWorkingDirectory.ShouldBeSameAs(_postGenWorkingDirectory);
        }

        [Fact]
        public void Should_Seed_UseRelativePathForPostGenWorkingDirectory_Defaults()
        {
            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value.ShouldBeTrue();
        }

        [Fact]
        public void Should_Have_PostGenBrowseCommands()
        {
            _viewModel.BrowsePostGenCommandCommand.ShouldNotBeNull();
            _viewModel.BrowsePostGenWorkingDirectoryCommand.ShouldNotBeNull();
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
            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = false;
            _workingDirectory.Value = @"C:\projects\src";

            // Toggle on — should convert to relative
            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = true;

            _workingDirectory.Value.ShouldBe("src");
        }

        [Fact]
        public void Should_Convert_To_Absolute_When_Toggled_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            // Start with a relative path, toggle on
            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = true;
            _workingDirectory.Value = "src";

            // Toggle off — should convert to absolute
            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = false;

            _workingDirectory.Value.ShouldBe(@"C:\projects\src");
        }

        [Fact]
        public void Should_Not_Change_Already_Relative_Path_When_Toggled_On()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = true;
            _workingDirectory.Value = @"..\..\..\AllOverIt";

            // Toggle again — should not be re-resolved against process CWD
            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = true;

            _workingDirectory.Value.ShouldBe(@"..\..\..\AllOverIt");
        }

        [Fact]
        public void Should_Not_Change_Already_Absolute_Path_When_Toggled_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = false;
            _workingDirectory.Value = @"C:\tools";

            // Toggle again — should not be re-converted
            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = false;

            _workingDirectory.Value.ShouldBe(@"C:\tools");
        }
    }

    public class DocumentOpen : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Not_Rewrite_Absolute_WorkingDirectory_When_Toggle_Synced_To_Absolute()
        {
            // Simulates a document opened with an absolute working directory: the editor's
            // SetOriginalValues syncs UseRelativePath to match the loaded path BEFORE the page
            // view model is constructed (page VMs are created lazily on navigation). Regression
            // test: constructing the page must not run ToggleRelativePath and rewrite the loaded
            // absolute path to relative, and the checkbox must reflect the absolute format.
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");
            _workingDirectory.SetOriginalValue(@"C:\projects\src");
            _useRelativePath.SetOriginalValue(false);

            var pageViewModel = new PipelineViewModel(_store, _toolStatus, Substitute.For<IErrorDialogService>(), Substitute.For<ILogger<PipelineViewModel>>());

            _workingDirectory.Value.ShouldBe(@"C:\projects\src");
            pageViewModel.UseRelativePathForPreGenWorkingDirectory.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Not_Rewrite_Absolute_PostGen_WorkingDirectory_When_Toggle_Synced_To_Absolute()
        {
            // Same regression as above, but for the post-generation working directory.
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");
            _postGenWorkingDirectory.SetOriginalValue(@"C:\projects\src");
            _postGenUseRelativePath.SetOriginalValue(false);

            var pageViewModel = new PipelineViewModel(_store, _toolStatus, Substitute.For<IErrorDialogService>(), Substitute.For<ILogger<PipelineViewModel>>());

            _postGenWorkingDirectory.Value.ShouldBe(@"C:\projects\src");
            pageViewModel.UseRelativePathForPostGenWorkingDirectory.Value.ShouldBeFalse();
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

            var viewModel = new PipelineViewModel(_store, _toolStatus, Substitute.For<IErrorDialogService>(), Substitute.For<ILogger<PipelineViewModel>>());

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

            _viewModel.PreGenError!.ShouldContain("Command must not be empty");
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

    public class PostGenBrowseCommands : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Set_PostGenCommand_To_Filename_When_Browsed()
        {
            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\tools\deploy.cmd");
            });

            _viewModel.BrowsePostGenCommandCommand.Execute().Subscribe();

            _postGenCommand.Value.ShouldBe("deploy.cmd");
        }

        [Fact]
        public void Should_Populate_PostGenWorkingDirectory_When_Empty_And_Command_Browsed()
        {
            _postGenWorkingDirectory.Value = string.Empty;

            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\tools\deploy.cmd");
            });

            _viewModel.BrowsePostGenCommandCommand.Execute().Subscribe();

            _postGenWorkingDirectory.Value.ShouldBe(@"C:\tools");
        }

        [Fact]
        public void Should_Not_Overwrite_PostGenWorkingDirectory_When_Command_Browsed()
        {
            _postGenWorkingDirectory.Value = @"C:\existing";

            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\tools\deploy.cmd");
            });

            _viewModel.BrowsePostGenCommandCommand.Execute().Subscribe();

            _postGenWorkingDirectory.Value.ShouldBe(@"C:\existing");
        }

        [Fact]
        public void Should_Not_Change_PostGenCommand_When_Browse_Cancelled()
        {
            _postGenCommand.Value = "run.bat";

            _viewModel.BrowseCommandInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(null);
            });

            _viewModel.BrowsePostGenCommandCommand.Execute().Subscribe();

            _postGenCommand.Value.ShouldBe("run.bat");
        }

        [Fact]
        public void Should_Set_PostGenWorkingDirectory_When_Browsed()
        {
            _viewModel.BrowseWorkingDirectoryInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\work");
            });

            _viewModel.BrowsePostGenWorkingDirectoryCommand.Execute().Subscribe();

            _postGenWorkingDirectory.Value.ShouldBe(@"C:\work");
        }
    }

    public class PostGenRelativePathToggle : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Convert_To_Relative_When_Toggled_On()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = false;
            _postGenWorkingDirectory.Value = @"C:\projects\src";

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = true;

            _postGenWorkingDirectory.Value.ShouldBe("src");
        }

        [Fact]
        public void Should_Convert_To_Absolute_When_Toggled_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = true;
            _postGenWorkingDirectory.Value = "src";

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = false;

            _postGenWorkingDirectory.Value.ShouldBe(@"C:\projects\src");
        }

        [Fact]
        public void Should_Not_Change_Already_Relative_Path_When_Toggled_On()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = true;
            _postGenWorkingDirectory.Value = @"..\..\..\AllOverIt";

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = true;

            _postGenWorkingDirectory.Value.ShouldBe(@"..\..\..\AllOverIt");
        }

        [Fact]
        public void Should_Not_Change_Already_Absolute_Path_When_Toggled_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = false;
            _postGenWorkingDirectory.Value = @"C:\tools";

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = false;

            _postGenWorkingDirectory.Value.ShouldBe(@"C:\tools");
        }
    }

    public class PostGenError : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Be_Null_When_Toggle_Off()
        {
            _postGenEnabled.Value = false;
            _postGenCommand.Value = string.Empty;

            _viewModel.PostGenError.ShouldBeNull();
        }

        [Fact]
        public void Should_Be_Null_When_Command_Not_Empty()
        {
            _postGenEnabled.Value = true;
            _postGenCommand.Value = "deploy.cmd";

            _viewModel.PostGenError.ShouldBeNull();
        }

        [Fact]
        public void Should_Contain_Error_When_Toggle_On_And_Command_Empty()
        {
            _postGenEnabled.Value = true;
            _postGenCommand.Value = string.Empty;

            _viewModel.PostGenError!.ShouldContain("Command must not be empty");
        }

        [Fact]
        public void Should_Clear_When_Command_Filled()
        {
            _postGenEnabled.Value = true;
            _postGenCommand.Value = string.Empty;

            _viewModel.PostGenError.ShouldNotBeNull();

            _postGenCommand.Value = "deploy.cmd";

            _viewModel.PostGenError.ShouldBeNull();
        }

        [Fact]
        public void Should_Clear_When_Toggle_Turned_Off()
        {
            _postGenEnabled.Value = true;
            _postGenCommand.Value = string.Empty;

            _viewModel.PostGenError.ShouldNotBeNull();

            _postGenEnabled.Value = false;

            _viewModel.PostGenError.ShouldBeNull();
        }
    }

    public class PreGenWorkingDirectoryError : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Be_Null_When_Toggle_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _enabled.Value = false;
            _workingDirectory.Value = @"X:\DoesNotExist\Path";

            _viewModel.PreGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Be_Null_When_WorkingDirectory_Empty()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _enabled.Value = true;
            _workingDirectory.Value = string.Empty;

            _viewModel.PreGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Be_Null_When_Directory_Exists()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _enabled.Value = true;
            _workingDirectory.Value = Path.GetTempPath();

            _viewModel.PreGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Contain_Error_When_Directory_Does_Not_Exist()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _enabled.Value = true;
            _workingDirectory.Value = @"X:\DoesNotExist\Path";

            _viewModel.PreGenWorkingDirectoryError!.ShouldContain("Working directory was not found");
        }

        [Fact]
        public void Should_Resolve_Relative_Path_Against_Project_Directory()
        {
            _store.DocumentDirectory.Returns(@"C:\projects");

            _enabled.Value = true;
            _workingDirectory.Value = @"missing\sub";

            _viewModel.PreGenWorkingDirectoryError!.ShouldBe(@"Working directory was not found: C:\projects\missing\sub");
        }

        [Fact]
        public void Should_Not_Resolve_Relative_To_Project_When_Relative_Path_Disabled()
        {
            _store.DocumentDirectory.Returns(@"C:\projects");

            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = false;
            _enabled.Value = true;
            _workingDirectory.Value = @"not-a-real-working-directory";

            _viewModel.PreGenWorkingDirectoryError.ShouldBe("Working directory was not found: not-a-real-working-directory");
        }

        [Fact]
        public void Should_Validate_Absolute_Path_When_Relative_Path_Disabled()
        {
            _viewModel.UseRelativePathForPreGenWorkingDirectory.Value = false;
            _enabled.Value = true;
            _workingDirectory.Value = Path.GetTempPath();

            _viewModel.PreGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Clear_When_Directory_Fixed()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _enabled.Value = true;
            _workingDirectory.Value = @"X:\DoesNotExist\Path";

            _viewModel.PreGenWorkingDirectoryError.ShouldNotBeNull();

            _workingDirectory.Value = Path.GetTempPath();

            _viewModel.PreGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Clear_When_Toggle_Turned_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _enabled.Value = true;
            _workingDirectory.Value = @"X:\DoesNotExist\Path";

            _viewModel.PreGenWorkingDirectoryError.ShouldNotBeNull();

            _enabled.Value = false;

            _viewModel.PreGenWorkingDirectoryError.ShouldBeNull();
        }
    }

    public class PostGenWorkingDirectoryError : PipelineViewModelFixture
    {
        [Fact]
        public void Should_Be_Null_When_Toggle_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _postGenEnabled.Value = false;
            _postGenWorkingDirectory.Value = @"X:\DoesNotExist\Path";

            _viewModel.PostGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Be_Null_When_WorkingDirectory_Empty()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _postGenEnabled.Value = true;
            _postGenWorkingDirectory.Value = string.Empty;

            _viewModel.PostGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Be_Null_When_Directory_Exists()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _postGenEnabled.Value = true;
            _postGenWorkingDirectory.Value = Path.GetTempPath();

            _viewModel.PostGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Contain_Error_When_Directory_Does_Not_Exist()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _postGenEnabled.Value = true;
            _postGenWorkingDirectory.Value = @"X:\DoesNotExist\Path";

            _viewModel.PostGenWorkingDirectoryError!.ShouldContain("Working directory was not found");
        }

        [Fact]
        public void Should_Resolve_Relative_Path_Against_Project_Directory()
        {
            _store.DocumentDirectory.Returns(@"C:\projects");

            _postGenEnabled.Value = true;
            _postGenWorkingDirectory.Value = @"missing\sub";

            _viewModel.PostGenWorkingDirectoryError!.ShouldBe(@"Working directory was not found: C:\projects\missing\sub");
        }

        [Fact]
        public void Should_Not_Resolve_Relative_To_Project_When_Relative_Path_Disabled()
        {
            _store.DocumentDirectory.Returns(@"C:\projects");

            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = false;
            _postGenEnabled.Value = true;
            _postGenWorkingDirectory.Value = @"not-a-real-working-directory";

            _viewModel.PostGenWorkingDirectoryError.ShouldBe("Working directory was not found: not-a-real-working-directory");
        }

        [Fact]
        public void Should_Validate_Absolute_Path_When_Relative_Path_Disabled()
        {
            _viewModel.UseRelativePathForPostGenWorkingDirectory.Value = false;
            _postGenEnabled.Value = true;
            _postGenWorkingDirectory.Value = Path.GetTempPath();

            _viewModel.PostGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Clear_When_Directory_Fixed()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _postGenEnabled.Value = true;
            _postGenWorkingDirectory.Value = @"X:\DoesNotExist\Path";

            _viewModel.PostGenWorkingDirectoryError.ShouldNotBeNull();

            _postGenWorkingDirectory.Value = Path.GetTempPath();

            _viewModel.PostGenWorkingDirectoryError.ShouldBeNull();
        }

        [Fact]
        public void Should_Clear_When_Toggle_Turned_Off()
        {
            _store.DocumentFilePath.Returns(@"C:\projects\test.sds");

            _postGenEnabled.Value = true;
            _postGenWorkingDirectory.Value = @"X:\DoesNotExist\Path";

            _viewModel.PostGenWorkingDirectoryError.ShouldNotBeNull();

            _postGenEnabled.Value = false;

            _viewModel.PostGenWorkingDirectoryError.ShouldBeNull();
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

    public class RescanCommandError : PipelineViewModelFixture
    {
        [Fact]
        public async Task Should_Show_Error_Dialog()
        {
            var errorDialog = ErrorDialogTestHelpers.CreateErrorDialogSubstitute(out var interaction);

            var viewModel = new PipelineViewModel(_store, _toolStatus, errorDialog, Substitute.For<ILogger<PipelineViewModel>>());

            var exception = new InvalidOperationException("Rescan failed");

            _toolStatus
                .RescanAsync(Arg.Any<CancellationToken>())
                .ThrowsAsync(exception);

            var errorReceived = new TaskCompletionSource<ErrorInfo>(TaskCreationOptions.RunContinuationsAsynchronously);

            interaction.RegisterHandler(context =>
            {
                errorReceived.TrySetResult(context.Input);
                context.SetOutput(System.Reactive.Unit.Default);
            });

            // The failure is routed to the wired ThrownExceptions handler, which shows the error dialog.
            await ErrorDialogTestHelpers.ObserveExecuteIgnoringOutcomeAsync(viewModel.RescanToolsCommand, System.Reactive.Unit.Default);

            var capturedError = await ErrorDialogTestHelpers.WaitForCapturedErrorAsync(errorReceived.Task);

            capturedError.Title.ShouldBe("Tool Rescan failed");
            capturedError.Message.ShouldBe("Rescan failed");
        }
    }
}
