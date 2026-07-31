using Shouldly;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Features.Pipeline.PreGeneration;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Features.Pipeline.PreGeneration;

[Collection(nameof(ReactiveUIInitializer))]
public class PreGenerationConfigEditorFixture : IDisposable
{
    private readonly PreGenerationConfigEditor _editor = new();

    public class Construction : PreGenerationConfigEditorFixture
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
    }

    public class IsDirty : PreGenerationConfigEditorFixture
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
            _editor.SetOriginalValues(new PreGenerationConfig { Command = "cmd.exe" });

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class SetOriginalValues : PreGenerationConfigEditorFixture
    {
        [Fact]
        public void Should_Populate_All_Fields()
        {
            _editor.SetOriginalValues(new PreGenerationConfig
            {
                Enabled = true,
                Command = "build.cmd",
                Arguments = "/build",
                WorkingDirectory = @"C:\src",
                ContinueOnFailure = true
            });

            _editor.Enabled.Value.ShouldBeTrue();
            _editor.Command.Value.ShouldBe("build.cmd");
            _editor.Arguments.Value.ShouldBe("/build");
            _editor.WorkingDirectory.Value.ShouldBe(@"C:\src");
            _editor.ContinueOnFailure.Value.ShouldBeTrue();
        }

        [Fact]
        public void Should_Reset_Dirty_After_Edit()
        {
            _editor.SetOriginalValues(new PreGenerationConfig { Command = "a" });
            _editor.Command.Value = "b";

            _editor.IsDirty.ShouldBeTrue();

            _editor.SetOriginalValues(new PreGenerationConfig { Command = "b" });

            _editor.IsDirty.ShouldBeFalse();
        }
    }

    public class FlushTo : PreGenerationConfigEditorFixture
    {
        [Fact]
        public void Should_Write_All_Fields_To_Target()
        {
            _editor.SetOriginalValues(new PreGenerationConfig());

            _editor.Enabled.Value = true;
            _editor.Command.Value = "run.ps1";
            _editor.Arguments.Value = "-Force";
            _editor.WorkingDirectory.Value = @"C:\temp";
            _editor.ContinueOnFailure.Value = true;

            var target = new PreGenerationConfig();

            _editor.FlushTo(target);

            target.Enabled.ShouldBeTrue();
            target.Command.ShouldBe("run.ps1");
            target.Arguments.ShouldBe("-Force");
            target.WorkingDirectory.ShouldBe(@"C:\temp");
            target.ContinueOnFailure.ShouldBeTrue();
        }
    }

    public void Dispose()
    {
        _editor.Dispose();
        GC.SuppressFinalize(this);
    }
}
