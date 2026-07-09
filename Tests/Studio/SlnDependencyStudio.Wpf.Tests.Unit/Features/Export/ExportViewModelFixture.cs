using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Project.Stores;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Export;

[Collection(nameof(ReactiveUIInitializer))]
public class ExportViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly TrackableValue<string> _rootPath = new();
    private readonly IExportOptionsEditor _exportOptionsEditor = Substitute.For<IExportOptionsEditor>();
    private readonly ExportViewModel _viewModel;

    public ExportViewModelFixture()
    {
        _exportOptionsEditor.RootPath.Returns(_rootPath);
        _store.ExportOptionsEditor.Returns(_exportOptionsEditor);

        _viewModel = new ExportViewModel(_store);
    }

    public class Construction : ExportViewModelFixture
    {
        [Fact]
        public void Should_Expose_RootPath_From_Store_Editor()
        {
            _viewModel.RootPath.ShouldBeSameAs(_rootPath);
        }

        [Fact]
        public void Should_Have_BrowseExportPathCommand()
        {
            _viewModel.BrowseExportPathCommand.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Have_ValidationContext()
        {
            _viewModel.ValidationContext.ShouldNotBeNull();
        }
    }

    public class BrowseExportPathCommand : ExportViewModelFixture
    {
        [Fact]
        public void Should_Update_Path_When_Dialog_Returns_Value()
        {
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _viewModel.BrowseExportPathInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\Projects\Output");
            });

            _viewModel.BrowseExportPathCommand.Execute().Subscribe();

            _rootPath.Value.ShouldBe(@"Output");
        }

        [Fact]
        public void Should_Not_Change_Path_When_Dialog_Returns_Null()
        {
            _rootPath.SetOriginalValue(@"C:\Projects\existing");
            _rootPath.Value = @"C:\Projects\existing";
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _viewModel.BrowseExportPathInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(null);
            });

            _viewModel.BrowseExportPathCommand.Execute().Subscribe();

            _rootPath.Value.ShouldBe(@"C:\Projects\existing");
        }
    }

    public class Validation : ExportViewModelFixture
    {
        [Fact]
        public void Should_Fail_When_RootPath_Is_Empty()
        {
            _rootPath.SetOriginalValue(string.Empty);
            _rootPath.Value = string.Empty;

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Pass_When_RootPath_Is_Not_Empty()
        {
            _rootPath.SetOriginalValue(@"C:\Output");
            _rootPath.Value = @"C:\Output";

            _viewModel.ValidationContext.IsValid.ShouldBeTrue();
        }
    }
}
