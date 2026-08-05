using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Features.Project;
using SlnDependencyStudio.Wpf.Features.Project.Stores;
using SlnDependencyStudio.Wpf.Features.RecentProjects;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Project.Stores;

[Collection(nameof(ReactiveUIInitializer))]
public class ProjectDocumentStoreFixture
{
    private readonly IDependencyProjectService _projectService = Substitute.For<IDependencyProjectService>();
    private readonly IRecentProjectsStore _recentProjects = Substitute.For<IRecentProjectsStore>();
    private readonly ProjectDocumentStore _store;

    public ProjectDocumentStoreFixture()
    {
        _store = new ProjectDocumentStore(_projectService, _recentProjects, Substitute.For<ILogger<ProjectDocumentStore>>());
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
        public void Should_Have_DiagramOptionsEditor_Not_Null()
        {
            _store.DiagramOptionsEditor.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Have_RestoreSolutionEditor_Not_Null()
        {
            _store.RestoreSolutionEditor.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Have_PostGenerationEditor_Not_Null()
        {
            _store.PostGenerationEditor.ShouldNotBeNull();
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

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.HasDocument.ShouldBeTrue();
            _store.DocumentFilePath.ShouldBe("test.sds");
        }

        [Fact]
        public async Task Should_Compute_DocumentDirectory()
        {
            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Test", "Desc"));

            await _store.OpenAsync(@"C:\Projects\test.sds", TestContext.Current.CancellationToken);

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

            await _store.OpenAsync(@"C:\Projects\test.sds", TestContext.Current.CancellationToken);

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

            await _store.OpenAsync(@"C:\Projects\test.sds", TestContext.Current.CancellationToken);

            _store.ExportOptionsEditor.RootPath.Value.ShouldBe(@"..\Output");
        }

        [Fact]
        public async Task Should_Populate_ClearContents()
        {
            var document = CreateDocument("Test", "Desc");
            document.DiagramGenerator.Export.ClearContents = true;

            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync(@"C:\Projects\test.sds", TestContext.Current.CancellationToken);

            _store.ExportOptionsEditor.ClearContents.Value.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Populate_ImageFormats()
        {
            var document = CreateDocument("Test", "Desc");
            document.DiagramGenerator.Export.ImageFormats = [DiagramImageFormat.Png, DiagramImageFormat.Svg];

            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync(@"C:\Projects\test.sds", TestContext.Current.CancellationToken);

            _store.ExportOptionsEditor.ImageFormats.Items.ShouldContain(DiagramImageFormat.Png);
            _store.ExportOptionsEditor.ImageFormats.Items.ShouldContain(DiagramImageFormat.Svg);
        }

        [Fact]
        public async Task Should_Populate_DiagramOptionsEditor()
        {
            var document = CreateDocument("Test", "Desc");
            document.DiagramGenerator.Diagram.Formats = [DiagramFormat.Mermaid];

            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync(@"C:\Projects\test.sds", TestContext.Current.CancellationToken);

            _store.DiagramOptionsEditor.Formats.Items.ShouldContain(DiagramFormat.Mermaid);
            _store.DiagramOptionsEditor.Formats.Items.Count.ShouldBe(1);
        }

        [Fact]
        public async Task Should_Populate_MetadataEditor()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Test Project", "A description"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.MetadataEditor.ProjectName.Value.ShouldBe("Test Project");
            _store.MetadataEditor.Description.Value.ShouldBe("A description");
        }

        [Fact]
        public async Task Should_Leave_IsDirty_False()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Populate_RestoreSolutionEditor()
        {
            var document = CreateDocument("Test", "Desc");
            document.RestoreSolution = false;

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.RestoreSolutionEditor.RestoreSolution.Value.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Populate_PostGenerationEditor()
        {
            var document = CreateDocument("Test", "Desc");
            document.PostGeneration.Enabled = true;
            document.PostGeneration.Command = "deploy.cmd";
            document.PostGeneration.Arguments = "--prod";
            document.PostGeneration.WorkingDirectory = "dist";

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.PostGenerationEditor.Enabled.Value.ShouldBeTrue();
            _store.PostGenerationEditor.Command.Value.ShouldBe("deploy.cmd");
            _store.PostGenerationEditor.Arguments.Value.ShouldBe("--prod");
            _store.PostGenerationEditor.WorkingDirectory.Value.ShouldBe("dist");
        }

        [Fact]
        public async Task Should_Track_IsDirty_When_RestoreSolution_Changes()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.RestoreSolutionEditor.RestoreSolution.Value = false;

            _store.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Track_IsDirty_When_PostGeneration_Changes()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.PostGenerationEditor.Command.Value = "deploy.cmd";

            _store.IsDirty.ShouldBeTrue();
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

            await _store.OpenAsync("first.sds", TestContext.Current.CancellationToken);
            await _store.OpenAsync("second.sds", TestContext.Current.CancellationToken);

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

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

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

            await _store.OpenAsync("first.sds", TestContext.Current.CancellationToken);
            _store.MetadataEditor.ProjectName.Value = "Changed";

            _store.IsDirty.ShouldBeTrue();

            // Opening a different file discards changes — should be clean.
            await _store.OpenAsync("second.sds", TestContext.Current.CancellationToken);

            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Track_IsDirty_When_SolutionPath_Changes()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.SolutionOptionsEditor.SolutionPath.Value = @"C:\New\path.sln";

            _store.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Track_IsDirty_When_RootPath_Changes()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.ExportOptionsEditor.RootPath.Value = @"C:\New\Output";

            _store.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Track_IsDirty_When_Diagram_Formats_Change()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.DiagramOptionsEditor.Formats.Items.Add(DiagramFormat.D2);

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

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.MetadataEditor.ProjectName.Value = "Changed";

            _store.IsDirty.ShouldBeTrue();

            await _store.SaveAsync(TestContext.Current.CancellationToken);

            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Call_ProjectService()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            await _store.SaveAsync(TestContext.Current.CancellationToken);

            await _projectService.Received(1).SaveAsync(
                Arg.Any<DependencyProjectDocument>(),
                "test.sds",
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Should_Flush_RestoreSolution_And_PostGeneration_To_Document()
        {
            var document = CreateDocument("Name", "Desc");
            document.RestoreSolution = true;

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.RestoreSolutionEditor.RestoreSolution.Value = false;
            _store.PostGenerationEditor.Command.Value = "deploy.cmd";

            await _store.SaveAsync(TestContext.Current.CancellationToken);

            document.RestoreSolution.ShouldBeFalse();
            document.PostGeneration.Command.ShouldBe("deploy.cmd");
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

            await _store.OpenAsync("old.sds", TestContext.Current.CancellationToken);

            _store.MetadataEditor.ProjectName.Value = "Changed";

            await _store.SaveAsAsync("new.sds", TestContext.Current.CancellationToken);

            _store.DocumentFilePath.ShouldBe("new.sds");
            _store.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Add_To_Recent_Projects()
        {
            _projectService
                .OpenAsync("old.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("old.sds", TestContext.Current.CancellationToken);
            await _store.SaveAsAsync("new.sds", TestContext.Current.CancellationToken);

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

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

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

            await _store.OpenAsync("first.sds", TestContext.Current.CancellationToken);
            _store.Close();

            await _store.OpenAsync("second.sds", TestContext.Current.CancellationToken);

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

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

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

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.ExportOptionsEditor.RootPath.Value.ShouldBe(@"C:\Output");

            _store.Close();

            _store.ExportOptionsEditor.RootPath.Value.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task Should_Reset_DiagramOptionsEditor()
        {
            var document = CreateDocument("Name", "Desc");
            document.DiagramGenerator.Diagram.Formats = [DiagramFormat.D2];

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.DiagramOptionsEditor.Formats.Items.ShouldContain(DiagramFormat.D2);

            _store.Close();

            _store.DiagramOptionsEditor.Formats.Items.ShouldBeEmpty();
        }

        [Fact]
        public async Task Should_Reset_DocumentDirectory_To_Empty()
        {
            _projectService
                .OpenAsync(@"C:\Projects\test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync(@"C:\Projects\test.sds", TestContext.Current.CancellationToken);

            _store.DocumentDirectory.ShouldBe(@"C:\Projects");

            _store.Close();

            _store.DocumentDirectory.ShouldBe(string.Empty);
        }

        [Fact]
        public async Task Should_Reset_RestoreSolutionEditor()
        {
            var document = CreateDocument("Name", "Desc");
            document.RestoreSolution = false;

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.RestoreSolutionEditor.RestoreSolution.Value.ShouldBeFalse();

            _store.Close();

            _store.RestoreSolutionEditor.RestoreSolution.Value.ShouldBeTrue();
        }

        [Fact]
        public async Task Should_Reset_PostGenerationEditor()
        {
            var document = CreateDocument("Name", "Desc");
            document.PostGeneration.Enabled = true;
            document.PostGeneration.Command = "deploy.cmd";

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.PostGenerationEditor.Enabled.Value.ShouldBeTrue();

            _store.Close();

            _store.PostGenerationEditor.Enabled.Value.ShouldBeFalse();
        }
    }

    public class BuildDocument : ProjectDocumentStoreFixture
    {
        [Fact]
        public async Task Should_Flush_RestoreSolution_And_PostGeneration_To_Document()
        {
            var document = CreateDocument("Name", "Desc");
            document.RestoreSolution = true;

            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(document);

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.RestoreSolutionEditor.RestoreSolution.Value = false;
            _store.PostGenerationEditor.Command.Value = "deploy.cmd";

            var built = _store.BuildDocument();

            built.RestoreSolution.ShouldBeFalse();
            built.PostGeneration.Command.ShouldBe("deploy.cmd");
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

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);
            _store.HasDocument.ShouldBeTrue();

            _store.Close();
            _store.HasDocument.ShouldBeFalse();

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);
            _store.HasDocument.ShouldBeTrue();
        }
    }

    public class IsTransitioning : ProjectDocumentStoreFixture
    {
        [Fact]
        public void Should_Be_False_On_Construction()
        {
            _store.IsTransitioning.ShouldBeFalse();
        }

        [Fact]
        public async Task Should_Be_False_After_OpenAsync_Completes()
        {
            _projectService
                .OpenAsync("test.sds", Arg.Any<CancellationToken>())
                .Returns(CreateDocument("Name", "Desc"));

            await _store.OpenAsync("test.sds", TestContext.Current.CancellationToken);

            _store.IsTransitioning.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_False_After_Close_Completes()
        {
            _store.Close();

            _store.IsTransitioning.ShouldBeFalse();
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
