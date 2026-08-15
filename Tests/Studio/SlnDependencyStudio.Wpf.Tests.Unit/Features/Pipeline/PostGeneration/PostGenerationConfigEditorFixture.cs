using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Features.Pipeline.PostGeneration;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Pipeline.PostGeneration;

[Collection(nameof(ReactiveUIInitializer))]
public class PostGenerationConfigEditorFixture : IDisposable
{
    private readonly PostGenerationConfigEditor _editor = new(Substitute.For<ILogger<PostGenerationConfigEditor>>());

    public class Construction : PostGenerationConfigEditorFixture
    {
        [Fact]
        public void Should_Seed_Enabled_With_False()
        {
            _editor.Enabled.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Seed_Command_With_Empty()
        {
            _editor.Command.Value.ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Seed_UseRelativePath_With_True()
        {
            _editor.UseRelativePath.Value.ShouldBeTrue();
        }
    }

    public class IsDirty : PostGenerationConfigEditorFixture
    {
        [Fact]
        public void Should_Be_False_After_Construction()
        {
            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_True_When_Enabled_Changes()
        {
            _editor.Enabled.Value = true;

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_True_When_Command_Changes()
        {
            _editor.Command.Value = "cmd.exe";

            _editor.IsDirty.ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_When_Matching_Baseline()
        {
            _editor.SetOriginalValues(new PostGenerationConfig { Command = "cmd.exe" });

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalValues : PostGenerationConfigEditorFixture
    {
        [Fact]
        public void Should_Populate_All_Fields()
        {
            _editor.SetOriginalValues(new PostGenerationConfig
            {
                Enabled = true,
                Command = "deploy.cmd",
                Arguments = "--prod",
                WorkingDirectory = @"C:\dist"
            });

            _editor.Enabled.Value.ShouldBeTrue();
            _editor.Command.Value.ShouldBe("deploy.cmd");
            _editor.Arguments.Value.ShouldBe("--prod");
            _editor.WorkingDirectory.Value.ShouldBe(@"C:\dist");
        }

        [Fact]
        public void Should_Reset_Dirty_After_Edit()
        {
            _editor.SetOriginalValues(new PostGenerationConfig { Command = "a" });
            _editor.Command.Value = "b";

            _editor.IsDirty.ShouldBeTrue();

            _editor.SetOriginalValues(new PostGenerationConfig { Command = "b" });

            _editor.IsDirty.ShouldBeFalse();
        }

        [Fact]
        public void Should_Sync_UseRelativePath_To_Absolute_When_WorkingDirectory_Is_Absolute()
        {
            _editor.SetOriginalValues(new PostGenerationConfig { WorkingDirectory = @"C:\dist" });

            _editor.WorkingDirectory.Value.ShouldBe(@"C:\dist");
            _editor.UseRelativePath.Value.ShouldBeFalse();
        }

        [Fact]
        public void Should_Sync_UseRelativePath_To_Relative_When_WorkingDirectory_Is_Relative()
        {
            _editor.SetOriginalValues(new PostGenerationConfig { WorkingDirectory = @"..\dist" });

            _editor.WorkingDirectory.Value.ShouldBe(@"..\dist");
            _editor.UseRelativePath.Value.ShouldBeTrue();
        }
    }

    public class FlushTo : PostGenerationConfigEditorFixture
    {
        [Fact]
        public void Should_Write_All_Fields_To_Target()
        {
            _editor.SetOriginalValues(new PostGenerationConfig());

            _editor.Enabled.Value = true;
            _editor.Command.Value = "run.ps1";
            _editor.Arguments.Value = "-Force";
            _editor.WorkingDirectory.Value = @"C:\temp";

            var target = new PostGenerationConfig();

            _editor.FlushTo(target);

            target.Enabled.ShouldBeTrue();
            target.Command.ShouldBe("run.ps1");
            target.Arguments.ShouldBe("-Force");
            target.WorkingDirectory.ShouldBe(@"C:\temp");
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }
}
