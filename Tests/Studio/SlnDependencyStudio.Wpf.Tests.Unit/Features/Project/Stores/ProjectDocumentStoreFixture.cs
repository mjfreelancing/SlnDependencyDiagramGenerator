using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Features.Export;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.RecentProjects;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Project.Stores;

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
            _store.DocumentFilePath.ShouldBeNull();
        }

        [Fact]
        public void Should_Have_IsDirty_False()
        {
            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Have_SolutionOptionsEditor_Not_Null()
        {
            _store.SolutionOptionsEditor.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Have_ExportOptionsEditor_Not_Null()
        {
            _store.ExportOptionsEditor.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Have_UseRelativePath_True_By_Default()
        {
            _store.SolutionOptionsEditor.UseRelativePath.Value.ShouldBeTrue();
            _store.ExportOptionsEditor.UseRelativePath.Value.ShouldBeTrue();
        }

        [Fact]
        public void Should_Have_DocumentDirectory_Empty()
        {
            _store.DocumentDirectory.ShouldBe(string.Empty);
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
            _store.DocumentFilePath.ShouldBe("test.sds");
        }

        [Fact]
        public async Task Should_Compute_DocumentDirectory()
        {
            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Test", "Desc"));

            await _store.OpenAsync(@"C:\Projects\test.sds");

            _store.DocumentDirectory.ShouldBe(@"C:\Projects");
        }

        [Fact]
        public async Task Should_Populate_SolutionOptionsEditor()
        {
            var document = CreateDocument("Test", "Desc");
            document.DiagramGenerator.Solution.SolutionPath = @"..\MySolution.sln";

            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync(@"C:\Projects\test.sds");

            _store.SolutionOptionsEditor.SolutionPath.Value.ShouldBe(@"..\MySolution.sln");
        }

        [Fact]
        public async Task Should_Populate_ExportOptionsEditor()
        {
            var document = CreateDocument("Test", "Desc");
            document.DiagramGenerator.Export.RootPath = @"..\Output";

            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync(@"C:\Projects\test.sds");

            _store.ExportOptionsEditor.RootPath.Value.ShouldBe(@"..\Output");
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

            _store.DocumentFilePath.ShouldBe("second.sds");
            _store.MetadataEditor.ProjectName.Value.ShouldBe("Second");
            _store.MetadataEditor.Description.Value.ShouldBe("SecondDesc");
        }

        [Fact]
        public async Task Should_Add_To_Recent_Projects()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds");

            _recentProjects.Received(1).Add("test.sds");
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

        [Fact]
        public async Task Should_Track_IsDirty_When_SolutionPath_Changes()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds");

            _store.SolutionOptionsEditor.SolutionPath.Value = @"C:\New\path.sln";

            _store.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Track_IsDirty_When_RootPath_Changes()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds");

            _store.ExportOptionsEditor.RootPath.Value = @"C:\New\Output";

            _store.IsDirty.ShouldBeTrue();
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

            _store.DocumentFilePath.ShouldBe("new.sds");
            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Add_To_Recent_Projects()
        {
            _projectService
                .OpenAsync("old.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("old.sds");
            await _store.SaveAsAsync("new.sds");

            _recentProjects.Received(1).Add("new.sds");
        }
    }

    public class Close : ProjectDocumentStoreFixture
    {
        [Fact]
        public void Should_Reset_All_State_When_Nothing_Loaded()
        {
            _store.Close();

            _store.HasDocument.ShouldBeFalse();
            _store.DocumentFilePath.ShouldBeNull();
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
            _store.DocumentFilePath.ShouldBeNull();
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
            _store.DocumentFilePath.ShouldBe("second.sds");
            _store.MetadataEditor.ProjectName.Value.ShouldBe("Second");
        }

        [Fact]
        public async Task Should_Reset_SolutionOptionsEditor()
        {
            var document = CreateDocument("Name", "Desc");
            document.DiagramGenerator.Solution.SolutionPath = @"C:\MySolution.sln";

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds");

            _store.SolutionOptionsEditor.SolutionPath.Value.ShouldBe(@"C:\MySolution.sln");

            _store.Close();

            _store.SolutionOptionsEditor.SolutionPath.Value.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task Should_Reset_ExportOptionsEditor()
        {
            var document = CreateDocument("Name", "Desc");
            document.DiagramGenerator.Export.RootPath = @"C:\Output";

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds");

            _store.ExportOptionsEditor.RootPath.Value.ShouldBe(@"C:\Output");

            _store.Close();

            _store.ExportOptionsEditor.RootPath.Value.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task Should_Reset_DocumentDirectory_To_Empty()
        {
            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync(@"C:\Projects\test.sds");

            _store.DocumentDirectory.ShouldBe(@"C:\Projects");

            _store.Close();

            _store.DocumentDirectory.ShouldBe(string.Empty);
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
