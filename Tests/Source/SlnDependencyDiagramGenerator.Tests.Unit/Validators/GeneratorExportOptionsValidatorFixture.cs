using Shouldly;
using SlnDependencyDiagramGenerator.Config;
using SlnDependencyDiagramGenerator.Validators;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Validators;

public class GeneratorExportOptionsValidatorFixture
{
    public class Validate : GeneratorExportOptionsValidatorFixture
    {
        [Fact]
        public void Should_Return_No_Errors_For_A_Valid_Model()
        {
            var model = new GeneratorExportOptions
            {
                RootPath = "output",
                ImageFormats = [DiagramImageFormat.Png, DiagramImageFormat.Svg]
            };

            var validator = new GeneratorExportOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_An_Error_When_Root_Path_Is_Null()
        {
            var model = new GeneratorExportOptions
            {
                RootPath = null!,
                ImageFormats = [DiagramImageFormat.Png]
            };

            var validator = new GeneratorExportOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "RootPath");
        }

        [Fact]
        public void Should_Return_An_Error_When_Root_Path_Is_Empty()
        {
            var model = new GeneratorExportOptions
            {
                RootPath = string.Empty,
                ImageFormats = [DiagramImageFormat.Png]
            };

            var validator = new GeneratorExportOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "RootPath");
        }

        [Fact]
        public void Should_Return_An_Error_When_Image_Formats_Is_Null()
        {
            var model = new GeneratorExportOptions
            {
                RootPath = "output",
                ImageFormats = null!
            };

            var validator = new GeneratorExportOptionsValidator();
            var result = validator.Validate(model);

            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldContain(item => item.PropertyName == "ImageFormats");
        }
    }
}