using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.RecentProjects;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Project;

[Collection(nameof(ReactiveUIInitializer))]
public class ProjectDocumentStoreFixture
{
    private readonly IDependencyProjectService _projectService = Substitute.For<IDependencyProjectService>();
    private readonly IRecentProjectsService _recentProjects = Substitute.For<IRecentProjectsService>();
    private readonly ProjectDocumentStore _store;

    public ProjectDocumentStoreFixture()
    {
        _store = new ProjectDocumentStore(_projectService, _recentProjects);
    }

    public class Construction : ProjectDocumentStoreFixture
    {
        [Fact]
        public void Should_Have_HasDocument_False()
        {
            _store.HasDocument.ShouldBeFalse();
        }

        [Fact]
        public void Should_Have_CurrentFilePath_Null()
        {
            _store.CurrentFilePath.ShouldBeNull();
        }

        [Fact]
        public void Should_Have_IsDirty_False()
        {
            _store.IsDirty.ShouldBeFalse();
        }
    }

    public class OpenAsync : ProjectDocumentStoreFixture
    {
        [Fact]
        public async Task Should_Set_HasDocument_And_CurrentFilePath()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Test", "Desc"));

            await _store.OpenAsync("test.sds");

            _store.HasDocument.ShouldBeTrue();
            _store.CurrentFilePath.ShouldBe("test.sds");
        }

        [Fact]
        public async Task Should_Populate_MetadataEditor()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Test Project", "A description"));

            await _store.OpenAsync("test.sds");

            _store.MetadataEditor.ProjectName.Value.ShouldBe("Test Project");
            _store.MetadataEditor.Description.Value.ShouldBe("A description");
        }

        [Fact]
        public async Task Should_Leave_IsDirty_False()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds");

            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Replace_Previous_Document()
        {
            _projectService
                .OpenAsync("first.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("First", "FirstDesc"));

            _projectService
                .OpenAsync("second.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Second", "SecondDesc"));

            await _store.OpenAsync("first.sds");
            await _store.OpenAsync("second.sds");

            _store.CurrentFilePath.ShouldBe("second.sds");
            _store.MetadataEditor.ProjectName.Value.ShouldBe("Second");
            _store.MetadataEditor.Description.Value.ShouldBe("SecondDesc");
        }

        [Fact]
        public async Task Should_Be_Clean_When_Opening_Different_File_After_Edit()
        {
            _projectService
                .OpenAsync("first.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("First", "Desc"));

            _projectService
                .OpenAsync("second.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Second", "Desc"));

            await _store.OpenAsync("first.sds");
            _store.MetadataEditor.ProjectName.Value = "Changed";

            _store.IsDirty.ShouldBeTrue();

            // Opening a different file discards changes — should be clean.
            await _store.OpenAsync("second.sds");

            _store.IsDirty.ShouldBeFalse();
        }
    }

    public class SaveAsync : ProjectDocumentStoreFixture
    {
        [Fact]
        public async Task Should_Mark_Clean_After_Edit()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds");

            _store.MetadataEditor.ProjectName.Value = "Changed";

            _store.IsDirty.ShouldBeTrue();

            await _store.SaveAsync();

            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Call_ProjectService()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds");

            await _store.SaveAsync();

            await _projectService.Received(1).SaveAsync(
                Arg.Any<DependencyProjectDocument>(),
                "test.sds",
                Arg.Any<CancellationToken>());
        }
    }

    public class SaveAsAsync : ProjectDocumentStoreFixture
    {
        [Fact]
        public async Task Should_Update_CurrentFilePath_And_Mark_Clean()
        {
            _projectService
                .OpenAsync("old.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("old.sds");

            _store.MetadataEditor.ProjectName.Value = "Changed";

            await _store.SaveAsAsync("new.sds");

            _store.CurrentFilePath.ShouldBe("new.sds");
            _store.IsDirty.ShouldBeFalse();
        }
    }

    public class Close : ProjectDocumentStoreFixture
    {
        [Fact]
        public void Should_Reset_All_State_When_Nothing_Loaded()
        {
            _store.Close();

            _store.HasDocument.ShouldBeFalse();
            _store.CurrentFilePath.ShouldBeNull();
            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Reset_All_State_After_Open_And_Edit()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds");

            _store.MetadataEditor.ProjectName.Value = "Changed";

            _store.Close();

            _store.HasDocument.ShouldBeFalse();
            _store.CurrentFilePath.ShouldBeNull();
            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Allow_Reopen_After_Close()
        {
            _projectService
                .OpenAsync("first.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("First", "Desc"));

            _projectService
                .OpenAsync("second.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Second", "Desc"));

            await _store.OpenAsync("first.sds");
            _store.Close();

            await _store.OpenAsync("second.sds");

            _store.HasDocument.ShouldBeTrue();
            _store.CurrentFilePath.ShouldBe("second.sds");
            _store.MetadataEditor.ProjectName.Value.ShouldBe("Second");
        }
    }

    public class HasDocument : ProjectDocumentStoreFixture
    {
        [Fact]
        public async Task Should_Toggle_Through_Open_Close_Open_Cycle()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            _store.HasDocument.ShouldBeFalse();

            await _store.OpenAsync("test.sds");
            _store.HasDocument.ShouldBeTrue();

            _store.Close();
            _store.HasDocument.ShouldBeFalse();

            await _store.OpenAsync("test.sds");
            _store.HasDocument.ShouldBeTrue();
        }
    }

    private static DependencyProjectDocument CreateDocument(string name, string description)
    {
        return new DependencyProjectDocument
        {
            Metadata = new DependencyProjectMetadata
            {
                ProjectName = name,
                Description = description
            }
        };
    }
}
