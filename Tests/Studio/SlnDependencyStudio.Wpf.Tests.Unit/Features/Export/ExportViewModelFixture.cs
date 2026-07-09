using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using System.IO;
using System.Reactive.Linq;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Export;

[Collection(nameof(ReactiveUIInitializer))]
public class ExportViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly TrackableValue<string> _rootPath = new();
    private readonly TrackableValue<bool> _useRelativePath = new();
    private readonly IExportOptionsEditor _exportOptionsEditor = Substitute.For<IExportOptionsEditor>();
    private readonly ExportViewModel _viewModel;

    public ExportViewModelFixture()
    {
        _exportOptionsEditor.RootPath.Returns(_rootPath);
        _exportOptionsEditor.UseRelativePath.Returns(_useRelativePath);
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
        public void Should_Expose_UseRelativePath_From_Store_Editor()
        {
            _viewModel.UseRelativePath.ShouldBeSameAs(_useRelativePath);
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
        public void Should_Store_Relative_Path_When_UseRelativePath_Is_True()
        {
            _useRelativePath.SetOriginalValue(true);
            _useRelativePath.Value = true;
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _viewModel.BrowseExportPathInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\Projects\Output");
            });

            _viewModel.BrowseExportPathCommand.Execute().Subscribe();

            _rootPath.Value.ShouldBe(@"Output");
        }

        [Fact]
        public void Should_Store_Absolute_Path_When_UseRelativePath_Is_False()
        {
            _useRelativePath.SetOriginalValue(false);
            _useRelativePath.Value = false;
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _viewModel.BrowseExportPathInteraction.RegisterHandler(ctx =>
            {
                ctx.SetOutput(@"C:\Projects\Output");
            });

            _viewModel.BrowseExportPathCommand.Execute().Subscribe();

            _rootPath.Value.ShouldBe(@"C:\Projects\Output");
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

    public class UseRelativePathToggle : ExportViewModelFixture
    {
        [Fact]
        public void Should_Convert_To_Absolute_When_Unchecked()
        {
            _useRelativePath.SetOriginalValue(true);
            _useRelativePath.Value = true;
            _rootPath.SetOriginalValue(@"..\Output");
            _rootPath.Value = @"..\Output";
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _useRelativePath.Value = false;

            _rootPath.Value.ShouldBe(Path.GetFullPath(@"C:\Projects\..\Output"));
        }

        [Fact]
        public void Should_Convert_To_Relative_When_Checked()
        {
            _useRelativePath.SetOriginalValue(false);
            _useRelativePath.Value = false;
            _rootPath.SetOriginalValue(@"C:\Projects\Output");
            _rootPath.Value = @"C:\Projects\Output";
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _useRelativePath.Value = true;

            _rootPath.Value.ShouldBe(@"Output");
        }

        [Fact]
        public void Should_Not_Change_Empty_Path()
        {
            _useRelativePath.SetOriginalValue(true);
            _useRelativePath.Value = true;
            _rootPath.SetOriginalValue(string.Empty);
            _rootPath.Value = string.Empty;
            _store.DocumentDirectory.Returns(@"C:\Projects");

            _useRelativePath.Value = false;

            _rootPath.Value.ShouldBe(string.Empty);
        }
    }
}
