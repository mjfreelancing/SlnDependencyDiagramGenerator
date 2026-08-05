using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Features.Project;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Project;

[Collection(nameof(ReactiveUIInitializer))]
public class ProjectMetadataEditorFixture : IDisposable
{
    private readonly ProjectMetadataEditor _editor = new(Substitute.For<ILogger<ProjectMetadataEditor>>());

    public class Construction : ProjectMetadataEditorFixture
    {
        [Fact]
        public void Should_Seed_ProjectName_With_Empty_String()
        {
            _editor.ProjectName.Value.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Seed_Description_With_Empty_String()
        {
            _editor.Description.Value.ShouldBe(string.Empty);
        }
    }

    public class IsDirty : ProjectMetadataEditorFixture
    {
        [Fact]
        public void Should_Be_False_After_Construction()
        {
            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_True_When_ProjectName_Changes()
        {
            _editor.ProjectName.Value = "Changed";

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_True_When_Description_Changes()
        {
            _editor.Description.Value = "Changed";

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_When_Both_Match_Baseline()
        {
            _editor.SetOriginalValues(CreateMetadata("Name", "Desc"));

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalValues : ProjectMetadataEditorFixture
    {
        [Fact]
        public void Should_Reset_Dirty_After_Edit()
        {
            _editor.SetOriginalValues(CreateMetadata("Name", "Desc"));

            _editor.ProjectName.Value = "NewName";

            _editor.IsDirty.ShouldBeTrue();

            _editor.SetOriginalValues(CreateMetadata("NewName", "Desc"));

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class FlushTo : ProjectMetadataEditorFixture
    {
        [Fact]
        public void Should_Write_Current_Values_To_Target()
        {
            _editor.SetOriginalValues(CreateMetadata("Name", "Desc"));

            _editor.ProjectName.Value = "NewName";
            _editor.Description.Value = "NewDesc";

            var target = new DependencyProjectMetadata();

            _editor.FlushTo(target);

            target.ProjectName.ShouldBe("NewName");
            target.Description.ShouldBe("NewDesc");
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }

    private static DependencyProjectMetadata CreateMetadata(string name, string description)
    {
        return new DependencyProjectMetadata
        {
            ProjectName = name,
            Description = description
        };
    }
}
