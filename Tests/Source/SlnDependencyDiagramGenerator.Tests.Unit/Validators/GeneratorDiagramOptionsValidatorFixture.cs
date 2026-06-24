using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Validators;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Validators;

public class GeneratorDiagramOptionsValidatorFixture
{
    public class Validate : GeneratorDiagramOptionsValidatorFixture
    {
        [Fact]
        public void Should_Return_No_Errors_For_A_Valid_D2_Format()
        {
            var model = CreateValidModel();

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_No_Errors_For_A_Valid_Mermaid_Format()
        {
            var model = CreateValidModel();
            model.Formats = [DiagramFormat.Mermaid];

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_No_Errors_For_Both_Formats()
        {
            var model = CreateValidModel();
            model.Formats = [DiagramFormat.D2, DiagramFormat.Mermaid];

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_An_Error_When_Formats_Is_Null()
        {
            var model = CreateValidModel();
            model.Formats = null!;

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Formats");
        }

        [Fact]
        public void Should_Return_An_Error_When_Formats_Is_Empty()
        {
            var model = CreateValidModel();
            model.Formats = [];

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Formats");
        }

        [Fact]
        public void Should_Return_An_Error_When_Framework_Style_Is_Null()
        {
            var model = new GeneratorDiagramOptions
            {
                Direction = GeneratorDiagramOptions.DiagramDirection.LR,
                GroupName = "Group",
                GroupNameAlias = "group",
                FrameworkStyle = null!,
                PackageStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                TransitiveStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                Grouping = new GeneratorDiagramOptions.GroupingOptions
                {
                    Enabled = true,
                    BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                    {
                        Fill = "#EEEEEE",
                        Opacity = 1
                    }
                },
                Formats = [DiagramFormat.D2]
            };

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "FrameworkStyle");
        }

        [Fact]
        public void Should_Return_An_Error_When_Package_Style_Is_Null()
        {
            var model = new GeneratorDiagramOptions
            {
                Direction = GeneratorDiagramOptions.DiagramDirection.LR,
                GroupName = "Group",
                GroupNameAlias = "group",
                FrameworkStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                PackageStyle = null!,
                TransitiveStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                Grouping = new GeneratorDiagramOptions.GroupingOptions
                {
                    Enabled = true,
                    BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                    {
                        Fill = "#EEEEEE",
                        Opacity = 1
                    }
                },
                Formats = [DiagramFormat.D2]
            };

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "PackageStyle");
        }

        [Fact]
        public void Should_Return_An_Error_When_Transitive_Style_Is_Null()
        {
            var model = new GeneratorDiagramOptions
            {
                Direction = GeneratorDiagramOptions.DiagramDirection.LR,
                GroupName = "Group",
                GroupNameAlias = "group",
                FrameworkStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                PackageStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                TransitiveStyle = null!,
                Grouping = new GeneratorDiagramOptions.GroupingOptions
                {
                    Enabled = true,
                    BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                    {
                        Fill = "#EEEEEE",
                        Opacity = 1
                    }
                },
                Formats = [DiagramFormat.D2]
            };

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "TransitiveStyle");
        }

        [Fact]
        public void Should_Return_An_Error_When_Grouping_Is_Null()
        {
            var model = new GeneratorDiagramOptions
            {
                Direction = GeneratorDiagramOptions.DiagramDirection.LR,
                GroupName = "Group",
                GroupNameAlias = "group",
                FrameworkStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                PackageStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                TransitiveStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#FFFFFF",
                    Opacity = 0.8
                },
                Grouping = null!,
                Formats = [DiagramFormat.D2]
            };

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Grouping");
        }

        [Fact]
        public void Should_Return_An_Error_When_Grouping_Background_Style_Is_Null()
        {
            var grouping = new GeneratorDiagramOptions.GroupingOptions
            {
                BackgroundStyle = null!
            };

            var model = CreateValidModel(grouping: grouping);

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Grouping.BackgroundStyle");
        }

        [Fact]
        public void Should_Return_An_Error_When_Group_Name_Is_Empty()
        {
            var model = CreateValidModel();
            model.GroupName = string.Empty;

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "GroupName");
        }

        [Fact]
        public void Should_Return_An_Error_When_Group_Name_Alias_Is_Empty()
        {
            var model = CreateValidModel();
            model.GroupNameAlias = string.Empty;

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "GroupNameAlias");
        }

        [Fact]
        public void Should_Return_An_Error_When_Fill_Style_Fill_Is_Null()
        {
            var frameworkStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = null!,
                Opacity = 0.8
            };

            var model = CreateValidModel(frameworkStyle: frameworkStyle);

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "FrameworkStyle.Fill");
        }

        [Fact]
        public void Should_Return_An_Error_When_Fill_Style_Fill_Is_Empty()
        {
            var frameworkStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = string.Empty,
                Opacity = 0.8
            };

            var model = CreateValidModel(frameworkStyle: frameworkStyle);

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "FrameworkStyle.Fill");
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        public void Should_Return_No_Errors_When_Fill_Style_Opacity_Is_Within_Range(double opacity)
        {
            var frameworkStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#FFFFFF",
                Opacity = opacity
            };

            var model = CreateValidModel(frameworkStyle: frameworkStyle);

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeTrue();
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        public void Should_Return_An_Error_When_Fill_Style_Opacity_Is_Out_Of_Range(double opacity)
        {
            var frameworkStyle = new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#FFFFFF",
                Opacity = opacity
            };

            var model = CreateValidModel(frameworkStyle: frameworkStyle);

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "FrameworkStyle.Opacity");
        }

        [Fact]
        public void Should_Return_An_Error_When_Direction_Is_Invalid()
        {
            var model = CreateValidModel();
            model.Direction = (GeneratorDiagramOptions.DiagramDirection)999;

            var validator = new GeneratorDiagramOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "Direction");
        }
    }

    private static GeneratorDiagramOptions CreateValidModel(
        GeneratorDiagramOptions.FillStyle? frameworkStyle = null,
        GeneratorDiagramOptions.FillStyle? packageStyle = null,
        GeneratorDiagramOptions.FillStyle? transitiveStyle = null,
        GeneratorDiagramOptions.GroupingOptions? grouping = null)
    {
        return new GeneratorDiagramOptions
        {
            Direction = GeneratorDiagramOptions.DiagramDirection.LR,
            GroupName = "Group",
            GroupNameAlias = "group",
            FrameworkStyle = frameworkStyle ?? new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#FFFFFF",
                Opacity = 0.8
            },
            PackageStyle = packageStyle ?? new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#FFFFFF",
                Opacity = 0.8
            },
            TransitiveStyle = transitiveStyle ?? new GeneratorDiagramOptions.FillStyle
            {
                Fill = "#FFFFFF",
                Opacity = 0.8
            },
            Grouping = grouping ?? new GeneratorDiagramOptions.GroupingOptions
            {
                Enabled = true,
                BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = "#EEEEEE",
                    Opacity = 1
                }
            },
            Formats = [DiagramFormat.D2]
        };
    }
}