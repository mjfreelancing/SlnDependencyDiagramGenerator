using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Validators;
using Shouldly;
using System;
using System.IO;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Validators;

public class GeneratorProjectOptionsValidatorFixture
{
    public class Validate : GeneratorProjectOptionsValidatorFixture
    {
        [Fact]
        public void Should_Return_No_Errors_For_A_Valid_Solution_Path()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = CreateValidModel(solutionPath);

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeTrue();
                result.Errors.Count.ShouldBe(0);
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_No_Errors_For_A_Valid_Slnx_Solution_Path()
        {
            var solutionPath = CreateTempSolutionFilePath(".slnx");

            try
            {
                var model = CreateValidModel(solutionPath);

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeTrue();
                result.Errors.Count.ShouldBe(0);
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_For_An_Unsupported_Solution_Extension()
        {
            var solutionPath = CreateTempSolutionFilePath(".txt");

            try
            {
                var model = CreateValidModel(solutionPath);

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.ErrorMessage == "SolutionPath must reference a .sln or .slnx file.");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_Solution_Path_Is_Null()
        {
            var model = CreateValidModel(null!);

            var validator = new GeneratorProjectOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "SolutionPath");
        }

        [Fact]
        public void Should_Return_An_Error_When_Solution_Path_Is_Empty()
        {
            var model = CreateValidModel(string.Empty);

            var validator = new GeneratorProjectOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "SolutionPath");
        }

        [Fact]
        public void Should_Return_An_Error_When_Solution_File_Does_Not_Exist()
        {
            var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sln");
            var model = CreateValidModel(missingPath);

            var validator = new GeneratorProjectOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.ErrorMessage.Contains(missingPath, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Should_Return_An_Error_When_Regex_To_Include_Is_Null()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = new GeneratorProjectOptions
                {
                    SolutionPath = solutionPath,
                    RegexToInclude = null!,
                    RegexToExclude = [],
                    PackagesToExclude = [],
                    FrameworksToExclude = [],
                    Individual = new GeneratorProjectOptions.ProjectScope
                    {
                        Enabled = true,
                        IncludeDependencies = true,
                        TransitiveDepth = 0
                    },
                    All = new GeneratorProjectOptions.ProjectScope
                    {
                        Enabled = true,
                        IncludeDependencies = true,
                        TransitiveDepth = 0
                    }
                };

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "RegexToInclude");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_Regex_To_Include_Is_Empty()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = CreateValidModel(solutionPath, regexToInclude: []);

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "RegexToInclude");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_Regex_To_Exclude_Is_Null()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = new GeneratorProjectOptions
                {
                    SolutionPath = solutionPath,
                    RegexToInclude = [".*\\.csproj"],
                    RegexToExclude = null!,
                    PackagesToExclude = [],
                    FrameworksToExclude = [],
                    Individual = new GeneratorProjectOptions.ProjectScope
                    {
                        Enabled = true,
                        IncludeDependencies = true,
                        TransitiveDepth = 0
                    },
                    All = new GeneratorProjectOptions.ProjectScope
                    {
                        Enabled = true,
                        IncludeDependencies = true,
                        TransitiveDepth = 0
                    }
                };

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "RegexToExclude");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_Packages_To_Exclude_Is_Null()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = new GeneratorProjectOptions
                {
                    SolutionPath = solutionPath,
                    RegexToInclude = [".*\\.csproj"],
                    RegexToExclude = [],
                    PackagesToExclude = null!,
                    FrameworksToExclude = [],
                    Individual = new GeneratorProjectOptions.ProjectScope
                    {
                        Enabled = true,
                        IncludeDependencies = true,
                        TransitiveDepth = 0
                    },
                    All = new GeneratorProjectOptions.ProjectScope
                    {
                        Enabled = true,
                        IncludeDependencies = true,
                        TransitiveDepth = 0
                    }
                };

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "PackagesToExclude");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_Frameworks_To_Exclude_Is_Null()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = new GeneratorProjectOptions
                {
                    SolutionPath = solutionPath,
                    RegexToInclude = [".*\\.csproj"],
                    RegexToExclude = [],
                    PackagesToExclude = [],
                    FrameworksToExclude = null!,
                    Individual = new GeneratorProjectOptions.ProjectScope
                    {
                        Enabled = true,
                        IncludeDependencies = true,
                        TransitiveDepth = 0
                    },
                    All = new GeneratorProjectOptions.ProjectScope
                    {
                        Enabled = true,
                        IncludeDependencies = true,
                        TransitiveDepth = 0
                    }
                };

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "FrameworksToExclude");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_Individual_Is_Null()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = CreateValidModel(solutionPath);
                model.Individual = null!;

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "Individual");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_All_Is_Null()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = CreateValidModel(solutionPath);
                model.All = null!;

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "All");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_Individual_Transitive_Depth_Is_Negative()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = CreateValidModel(solutionPath);
                model.Individual = new GeneratorProjectOptions.ProjectScope
                {
                    Enabled = true,
                    IncludeDependencies = true,
                    TransitiveDepth = -1
                };

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "Individual.TransitiveDepth");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_An_Error_When_All_Transitive_Depth_Is_Negative()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = CreateValidModel(solutionPath);
                model.All = new GeneratorProjectOptions.ProjectScope
                {
                    Enabled = true,
                    IncludeDependencies = true,
                    TransitiveDepth = -1
                };

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeFalse();
                result.Errors.ShouldContain(item => item.PropertyName == "All.TransitiveDepth");
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }

        [Fact]
        public void Should_Return_No_Errors_When_Individual_Transitive_Depth_Is_Zero()
        {
            var solutionPath = CreateTempSolutionFilePath(".sln");

            try
            {
                var model = CreateValidModel(solutionPath);
                model.Individual = new GeneratorProjectOptions.ProjectScope
                {
                    Enabled = true,
                    IncludeDependencies = true,
                    TransitiveDepth = 0
                };

                var validator = new GeneratorProjectOptionsValidator();
                var result = validator.Validate(model);

                result.IsValid.ShouldBeTrue();
            }
            finally
            {
                File.Delete(solutionPath);
            }
        }
    }

    private static GeneratorProjectOptions CreateValidModel(
        string solutionPath,
        string[]? regexToInclude = null,
        string[]? regexToExclude = null,
        string[]? packagesToExclude = null,
        string[]? frameworksToExclude = null)
    {
        return new GeneratorProjectOptions
        {
            SolutionPath = solutionPath,
            RegexToInclude = regexToInclude ?? [".*\\.csproj"],
            RegexToExclude = regexToExclude ?? [],
            PackagesToExclude = packagesToExclude ?? [],
            FrameworksToExclude = frameworksToExclude ?? [],
            Individual = new GeneratorProjectOptions.ProjectScope
            {
                Enabled = true,
                IncludeDependencies = true,
                TransitiveDepth = 0
            },
            All = new GeneratorProjectOptions.ProjectScope
            {
                Enabled = true,
                IncludeDependencies = true,
                TransitiveDepth = 0
            }
        };
    }

    private static string CreateTempSolutionFilePath(string extension)
    {
        var solutionPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        File.WriteAllText(solutionPath, string.Empty);

        return solutionPath;
    }
}