using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyStudio.Shared.Config;
using SlnDependencyStudio.Wpf.Enumerations;
using SlnDependencyStudio.Wpf.Utils;
using System.IO;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Utils;

public class RelativePathRebaserFixture
{
    public class Rewrite : RelativePathRebaserFixture
    {
        [Fact]
        public void Should_Convert_Relative_To_Absolute()
        {
            RelativePathRebaser
                .Rewrite(@"..\src", @"C:\projects", @"D:\backup", SaveAsRelativePathAction.ConvertToAbsolute)
                .ShouldBe(Path.GetFullPath(@"C:\projects\..\src"));
        }

        [Fact]
        public void Should_Leave_Absolute_Path_Unchanged_For_Both_Actions()
        {
            const string absolute = @"C:\projects\src";

            RelativePathRebaser
                .Rewrite(absolute, @"C:\projects", @"D:\backup", SaveAsRelativePathAction.ConvertToAbsolute)
                .ShouldBe(absolute);

            RelativePathRebaser
                .Rewrite(absolute, @"C:\projects", @"D:\backup", SaveAsRelativePathAction.RebaseRelative)
                .ShouldBe(absolute);
        }

        [Fact]
        public void Should_Leave_Empty_Value_Unchanged()
        {
            RelativePathRebaser
                .Rewrite(string.Empty, @"C:\projects", @"D:\backup", SaveAsRelativePathAction.ConvertToAbsolute)
                .ShouldBe(string.Empty);
        }

        [Fact]
        public void Should_Rebase_Relative_Path_To_New_Directory()
        {
            var absoluteTarget = Path.GetFullPath(@"C:\projects\..\src");

            RelativePathRebaser
                .Rewrite(@"..\src", @"C:\projects", @"C:\backups", SaveAsRelativePathAction.RebaseRelative)
                .ShouldBe(Path.GetRelativePath(@"C:\backups", absoluteTarget));
        }

        [Fact]
        public void Should_Fall_Back_To_Absolute_Across_Drives()
        {
            RelativePathRebaser
                .Rewrite(@"..\src", @"C:\projects", @"D:\backup", SaveAsRelativePathAction.RebaseRelative)
                .ShouldBe(Path.GetFullPath(@"C:\projects\..\src"));
        }
    }

    public class GetRelativePathFields : RelativePathRebaserFixture
    {
        [Fact]
        public void Should_Return_Only_Relative_Fields()
        {
            var document = new DependencyProjectDocument
            {
                DiagramGenerator = new DependencyGeneratorConfig
                {
                    Solution = new GeneratorSolutionOptions { SolutionPath = @"..\MyApp.sln" },
                    Export = new GeneratorExportOptions { RootPath = @"C:\Output" }
                },
                PreGeneration = new PreGenerationConfig { WorkingDirectory = "build" },
                PostGeneration = new PostGenerationConfig { WorkingDirectory = @"C:\dist" }
            };

            var fields = RelativePathRebaser.GetRelativePathFields(document);

            fields.Count.ShouldBe(2);
            fields.ShouldContain(field => field.Label == "Solution path" && field.Path == @"..\MyApp.sln");
            fields.ShouldContain(field => field.Label == "Pre-generation working directory" && field.Path == "build");
        }

        [Fact]
        public void Should_Return_Empty_When_All_Paths_Absolute_Or_Empty()
        {
            var document = new DependencyProjectDocument
            {
                DiagramGenerator = new DependencyGeneratorConfig
                {
                    Solution = new GeneratorSolutionOptions { SolutionPath = @"C:\MyApp.sln" },
                    Export = new GeneratorExportOptions { RootPath = @"C:\Output" }
                }
            };

            RelativePathRebaser.GetRelativePathFields(document).Count.ShouldBe(0);
        }
    }

    public class IsRelative : RelativePathRebaserFixture
    {
        [Fact]
        public void Should_Be_True_For_Relative_Path()
        {
            RelativePathRebaser.IsRelative(@"..\src").ShouldBeTrue();
        }

        [Fact]
        public void Should_Be_False_For_Absolute_Path()
        {
            RelativePathRebaser.IsRelative(@"C:\src").ShouldBeFalse();
        }

        [Fact]
        public void Should_Be_False_For_Empty_Value()
        {
            RelativePathRebaser.IsRelative(string.Empty).ShouldBeFalse();
        }
    }
}
