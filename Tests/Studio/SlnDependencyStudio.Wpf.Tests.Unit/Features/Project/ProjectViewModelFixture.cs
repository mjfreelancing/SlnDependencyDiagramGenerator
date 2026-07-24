using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Wpf.Controls;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Project.Stores;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Project;

[Collection(nameof(ReactiveUIInitializer))]
public class ProjectViewModelFixture
{
    private readonly IProjectDocumentStore _store = Substitute.For<IProjectDocumentStore>();
    private readonly IProjectMetadataEditor _metadataEditor = Substitute.For<IProjectMetadataEditor>();
    private readonly TrackableValue<string> _projectName = new();
    private readonly TrackableValue<string> _description = new();
    private readonly ProjectViewModel _viewModel;

    public ProjectViewModelFixture()
    {
        _projectName.SetOriginalValue(string.Empty);
        _description.SetOriginalValue(string.Empty);

        _metadataEditor.ProjectName.Returns(_projectName);
        _metadataEditor.Description.Returns(_description);
        _store.MetadataEditor.Returns(_metadataEditor);

        _viewModel = new ProjectViewModel(_store);
    }

    public class Construction : ProjectViewModelFixture
    {
        [Fact]
        public void Should_Expose_ProjectName_From_Store_Editor()
        {
            _viewModel.ProjectName.ShouldBeSameAs(_projectName);
        }

        [Fact]
        public void Should_Expose_Description_From_Store_Editor()
        {
            _viewModel.Description.ShouldBeSameAs(_description);
        }

        [Fact]
        public void Should_Have_ValidationContext()
        {
            _viewModel.ValidationContext.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Sync_DocumentFilePath_From_Store()
        {
            _store.DocumentFilePath.Returns(@"C:\Projects\test.sds");

            // Re-create with the configured store path so WhenAnyValue picks it up
            using var viewModel = new ProjectViewModel(_store);

            viewModel.DocumentFilePath.ShouldBe(@"C:\Projects\test.sds");
        }
    }

    public class Validation : ProjectViewModelFixture
    {
        [Fact]
        public void Should_Fail_When_ProjectName_Is_Empty()
        {
            _projectName.Value = string.Empty;

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Fail_When_ProjectName_Is_Whitespace()
        {
            _projectName.Value = "   ";

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Fail_When_ProjectName_Is_Null()
        {
            _projectName.Value = null!;

            _viewModel.ValidationContext.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void Should_Pass_When_ProjectName_Has_Value()
        {
            _projectName.Value = "My Project";

            _viewModel.ValidationContext.IsValid.ShouldBeTrue();
        }
    }

    public class Dispose : ProjectViewModelFixture
    {
        [Fact]
        public void Should_Not_Throw()
        {
            Should.NotThrow(_viewModel.Dispose);
        }
    }
}
