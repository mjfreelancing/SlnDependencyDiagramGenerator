using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Generator.ToolDetection;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Generator.ToolDetection;

public class ToolPathResolverFixture
{
    private static ToolPathResolver CreateSut(ToolPathOverridesProvider provider)
    {
        return new ToolPathResolver(provider);
    }

    public class GetEffectivePath : ToolPathResolverFixture
    {
        [Fact]
        public void Should_Return_Override_When_Configured()
        {
            var sut = CreateSut(() => new() { ["d2"] = @"C:\tools\d2.exe" });
            var result = sut.GetEffectivePath(DiagramFormat.D2);

            result.ShouldBe(@"C:\tools\d2.exe");
        }

        [Fact]
        public void Should_Return_ToolName_When_No_Override()
        {
            var sut = CreateSut(() => []);
            var result = sut.GetEffectivePath(DiagramFormat.D2);

            result.ShouldBe("d2");
        }
    }

    public class GetExplicitPath_ByFormat : ToolPathResolverFixture
    {
        [Fact]
        public void Should_Return_Override_When_Configured()
        {
            var sut = CreateSut(() => new() { ["d2"] = @"C:\tools\d2.exe" });
            var result = sut.GetExplicitPath(DiagramFormat.D2);

            result.ShouldBe(@"C:\tools\d2.exe");
        }

        [Fact]
        public void Should_Return_Null_When_No_Override()
        {
            var sut = CreateSut(() => []);
            var result = sut.GetExplicitPath(DiagramFormat.Mermaid);

            result.ShouldBeNull();
        }

        [Fact]
        public void Should_Read_Latest_Overrides_On_Each_Call()
        {
            var overrides = new Dictionary<string, string>();
            var sut = CreateSut(() => overrides);

            sut.GetExplicitPath(DiagramFormat.D2).ShouldBeNull();

            overrides["d2"] = @"C:\tools\d2.exe";

            sut.GetExplicitPath(DiagramFormat.D2).ShouldBe(@"C:\tools\d2.exe");
        }
    }

    public class GetExplicitPath_ByName : ToolPathResolverFixture
    {
        [Fact]
        public void Should_Return_Override_When_Configured()
        {
            var sut = CreateSut(() => new() { ["mmdc"] = @"C:\tools\mmdc.cmd" });
            var result = sut.GetExplicitPath("mmdc");

            result.ShouldBe(@"C:\tools\mmdc.cmd");
        }

        [Fact]
        public void Should_Return_Null_When_No_Override()
        {
            var sut = CreateSut(() => []);
            var result = sut.GetExplicitPath("d2");

            result.ShouldBeNull();
        }
    }

    public class GetToolName : ToolPathResolverFixture
    {
        [Fact]
        public void Should_Return_D2_ToolName()
        {
            var sut = CreateSut(() => []);
            sut.GetToolName(DiagramFormat.D2).ShouldBe("d2");
        }

        [Fact]
        public void Should_Return_Mermaid_ToolName()
        {
            var sut = CreateSut(() => []);
            sut.GetToolName(DiagramFormat.Mermaid).ShouldBe("mmdc");
        }
    }
}