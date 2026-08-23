using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.Solution;
using SlnDependencyStudio.Wpf.Tests.Unit.Support;
using System.IO;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Solution;

[Collection(nameof(ReactiveUIInitializer))]
public class SolutionViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly TrackableValue<string> _solutionPath = new();
    private readonly TrackableValue<bool> _useRelativePath = new();
    private readonly TrackableCollection<string> _regexToInclude = new();
    private readonly TrackableCollection<string> _regexToExclude = new();
    private readonly TrackableCollection<string> _packagesToExclude = new();
    private readonly TrackableCollection<string> _frameworksToExclude = new();
    private readonly TrackableValue<bool> _individualEnabled = new();
    private readonly TrackableValue<bool> _individualIncludeDependencies = new();
    private readonly TrackableValue<int> _individualTransitiveDepth = new();
    private readonly TrackableValue<bool> _allEnabled = new();
    private readonly TrackableValue<bool> _allIncludeDependencies = new();
    private readonly TrackableValue<int> _allTransitiveDepth = new();
    private readonly ISolutionOptionsEditor _solutionOptionsEditor = Substitute.For<ISolutionOptionsEditor>();
    private readonly SolutionViewModel _viewModel;

    public SolutionViewModelFixture()
    {
        _solutionPath.SetOriginalValue(string.Empty);
        _useRelativePath.SetOriginalValue(true);

        _individualEnabled.SetOriginalValue(true);
        _individualIncludeDependencies.SetOriginalValue(false);
        _individualTransitiveDepth.SetOriginalValue(0);
        _allEnabled.SetOriginalValue(false);
        _allIncludeDependencies.SetOriginalValue(false);
        _allTransitiveDepth.SetOriginalValue(0);

        _solutionOptionsEditor.SolutionPath.Returns(_solutionPath);
        _solutionOptionsEditor.UseRelativePath.Returns(_useRelativePath);
        _solutionOptionsEditor.RegexToInclude.Returns(_regexToInclude);
        _solutionOptionsEditor.RegexToExclude.Returns(_regexToExclude);
        _solutionOptionsEditor.PackagesToExclude.Returns(_packagesToExclude);
        _solutionOptionsEditor.FrameworksToExclude.Returns(_frameworksToExclude);
        _solutionOptionsEditor.IndividualEnabled.Returns(_individualEnabled);
        _solutionOptionsEditor.IndividualIncludeDependencies.Returns(_individualIncludeDependencies);
        _solutionOptionsEditor.IndividualTransitiveDepth.Returns(_individualTransitiveDepth);
        _solutionOptionsEditor.AllEnabled.Returns(_allEnabled);
        _solutionOptionsEditor.AllIncludeDependencies.Returns(_allIncludeDependencies);
        _solutionOptionsEditor.AllTransitiveDepth.Returns(_allTransitiveDepth);
        _store.SolutionOptionsEditor.Returns(_solutionOptionsEditor);

        _viewModel = new SolutionViewModel(_store);
    }

    public class Construction : SolutionViewModelFixture
    {
        [Fact]
        public void Should_Expose_SolutionPath_From_Store_Editor()
        {
            _viewModel.SolutionPath.ShouldBeSameAs(_solutionPath);
        }

        [Fact]
        public void Should_Expose_UseRelativePath_From_Store_Editor()
        {
            _viewModel.UseRelativePath.ShouldBeSameAs(_useRelativePath);
        }

        [Fact]
        public void Should_Have_BrowseSolutionPathCommand()
        {
            _viewModel.BrowseSolutionPathCommand.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Have_ValidationContext()
        {
            _viewModel.ValidationContext.ShouldNotBeNull();
        }
    }

    public class BrowseSolutionPathCommand : SolutionViewModelFixture
    {
        [Fact]
        public void Should_Store_Relative_Path_When_UseRelativePath_Is_True()
        {
            _useRelativePath.SetOriginalValue(true);
            _useRelativePath.Value = true;
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _viewModel.BrowseSolutionPathInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\Projects\selected.sln");
            });

            _viewModel.BrowseSolutionPathCommand.Execute().Subscribe();

            _solutionPath.Value.ShouldBe(@"selected.sln");
        }

        [Fact]
        public void Should_Store_Absolute_Path_When_UseRelativePath_Is_False()
        {
            _useRelativePath.SetOriginalValue(false);
            _useRelativePath.Value = false;
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _viewModel.BrowseSolutionPathInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\Projects\selected.sln");
            });

            _viewModel.BrowseSolutionPathCommand.Execute().Subscribe();

            _solutionPath.Value.ShouldBe(@"C:\Projects\selected.sln");
        }

        [Fact]
        public void Should_Not_Change_Path_When_Dialog_Returns_Null()
        {
            _solutionPath.SetOriginalValue(@"C:\Projects\existing.sln");
            _solutionPath.Value = @"C:\Projects\existing.sln";
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _viewModel.BrowseSolutionPathInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(null);
            });

            _viewModel.BrowseSolutionPathCommand.Execute().Subscribe();

            _solutionPath.Value.ShouldBe(@"C:\Projects\existing.sln");
        }
    }

    public class Validation : SolutionViewModelFixture
    {
        [Fact]
        public void Should_Fail_When_SolutionPath_Is_Empty()
        {
            _solutionPath.SetOriginalValue(string.Empty);
            _solutionPath.Value = string.Empty;

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Fail_When_File_Does_Not_Exist()
        {
            _solutionPath.SetOriginalValue(@"nonexistent.sln");
            _solutionPath.Value = @"nonexistent.sln";
            _store.DocumentDirectory.Returns(@"C:\Projects");

            // The resolved path C:\Projects\nonexistent.sln will not exist
            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Pass_When_File_Exists()
        {
            // Use the test assembly's own file which definitely exists
            var existingFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            _store.DocumentDirectory.Returns(Path.GetDirectoryName(existingFile)!);

            _solutionPath.SetOriginalValue(Path.GetFileName(existingFile));
            _solutionPath.Value = Path.GetFileName(existingFile);

            _viewModel.ValidationContext.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_Fail_When_No_Scope_Is_Enabled()
        {
            _solutionPath.SetOriginalValue(string.Empty);
            _solutionPath.Value = string.Empty;
            _individualEnabled.Value = false;
            _allEnabled.Value = false;

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }
    }

    public class UseRelativePathToggle : SolutionViewModelFixture
    {
        [Fact]
        public void Should_Convert_To_Absolute_When_Unchecked()
        {
            _useRelativePath.SetOriginalValue(true);
            _useRelativePath.Value = true;
            _solutionPath.SetOriginalValue(@"..\test.sln");
            _solutionPath.Value = @"..\test.sln";
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _useRelativePath.Value = false;

            _solutionPath.Value.ShouldBe(Path.GetFullPath(@"C:\Projects\..\test.sln"));
        }

        [Fact]
        public void Should_Convert_To_Relative_When_Checked()
        {
            _useRelativePath.SetOriginalValue(false);
            _useRelativePath.Value = false;
            _solutionPath.SetOriginalValue(@"C:\Projects\test.sln");
            _solutionPath.Value = @"C:\Projects\test.sln";
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _useRelativePath.Value = true;

            _solutionPath.Value.ShouldBe(@"test.sln");
        }

        [Fact]
        public void Should_Not_Change_Empty_Path()
        {
            _useRelativePath.SetOriginalValue(true);
            _useRelativePath.Value = true;
            _solutionPath.SetOriginalValue(string.Empty);
            _solutionPath.Value = string.Empty;
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _useRelativePath.Value = false;

            _solutionPath.Value.ShouldBe(string.Empty);
        }
    }
}
