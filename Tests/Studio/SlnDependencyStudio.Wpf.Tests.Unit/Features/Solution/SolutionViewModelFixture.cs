using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.Solution;
using System.IO;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Solution;

[Collection(nameof(ReactiveUIInitializer))]
public class SolutionViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly TrackableValue<string> _solutionPath = new();
    private readonly ISolutionOptionsEditor _solutionOptionsEditor = Substitute.For<ISolutionOptionsEditor>();
    private readonly SolutionViewModel _viewModel;

    public SolutionViewModelFixture()
    {
        _solutionOptionsEditor.SolutionPath.Returns(_solutionPath);
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
        public void Should_Update_Path_When_Dialog_Returns_Value()
        {
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _viewModel.BrowseSolutionPathInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\Projects\selected.sln");
            });

            _viewModel.BrowseSolutionPathCommand.Execute().Subscribe();

            _solutionPath.Value.ShouldBe(@"selected.sln");
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
    }
}
