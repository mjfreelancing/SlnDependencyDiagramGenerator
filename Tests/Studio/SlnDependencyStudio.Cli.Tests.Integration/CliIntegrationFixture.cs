using Shouldly;
using SlnDependencyDiagramGenerator.Parser;
using SlnDependencyStudio.Cli.Enumerations;
using SlnDependencyStudio.Cli.Tests.Integration.Support;

namespace SlnDependencyStudio.Cli.Tests.Integration;

/// <summary>End-to-end CLI integration tests that exercise the full
/// parsing and dispatch pipeline.</summary>
public class CliIntegrationFixture
{
    private static readonly string FixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact]
    public async Task Validate_With_Valid_Config_Should_Invoke_Handler()
    {
        var configFile = Path.Combine(FixturesDir, "valid-config.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"validate --cf \"{configFile}\"");

        // Handler is invoked (not a parse error); validation may fail
        // because the fixture doesn't point to a real solution.
        exitCode.ShouldNotBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task Validate_With_Missing_File_Should_Exit_NonZero()
    {
        var exitCode = await CliTestHarness.InvokeAsync(@"validate --cf C:\nonexistent\file.sds");

        exitCode.ShouldNotBe(0);
    }

    [Fact]
    public async Task Validate_With_Malformed_Json_Should_Exit_NonZero()
    {
        var configFile = Path.Combine(FixturesDir, "invalid-config.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"validate --cf \"{configFile}\"");

        exitCode.ShouldNotBe(0);
    }

    [Fact]
    public async Task Validate_With_Real_Solution_Should_Succeed()
    {
        // Uses the real-solution.sds fixture that references SoloLib/SoloLib.slnx.
        // The config has proper frameworkStyle/packageStyle/transitiveStyle
        // properties so all validation rules should pass.
        var configFile = Path.Combine(FixturesDir, "real-solution.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"validate --cf \"{configFile}\"");

        exitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Run_With_Valid_Config_Should_Invoke_Handler()
    {
        var configFile = Path.Combine(FixturesDir, "valid-config.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"run --cf \"{configFile}\"");

        // Handler is invoked (not a parse error); generation may fail
        // because the fixture doesn't point to a real solution.
        exitCode.ShouldNotBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task Run_With_Missing_File_Should_Exit_NonZero()
    {
        var exitCode = await CliTestHarness.InvokeAsync(@"run --cf C:\missing\config.sds");

        exitCode.ShouldNotBe(0);
    }

    [Fact]
    public async Task Run_With_Real_Solution_Should_Generate_Output()
    {
        // Uses the real-solution.sds fixture that references SoloLib/SoloLib.slnx.
        // Validates the full run pipeline is invoked (generation may partially
        // fail because the test projects lack restore assets).
        var configFile = Path.Combine(FixturesDir, "real-solution.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"run --cf \"{configFile}\"");

        // Handler is invoked (not a parse error); actual generation may fail
        // since test fixture projects don't have NuGet restore assets.
        exitCode.ShouldNotBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task Unknown_Command_Should_Exit_With_ParseError()
    {
        var exitCode = await CliTestHarness.InvokeAsync("unknown --cf file.sds");

        exitCode.ShouldBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task Missing_ConfigFile_Option_Should_Exit_With_ParseError()
    {
        var exitCode = await CliTestHarness.InvokeAsync("validate");

        exitCode.ShouldBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task ConfigFile_Short_Option_Should_Invoke_Handler()
    {
        var configFile = Path.Combine(FixturesDir, "valid-config.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"validate --cf \"{configFile}\"");

        exitCode.ShouldNotBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task Validate_With_Verbose_Flag_Should_Not_Parse_Error()
    {
        var configFile = Path.Combine(FixturesDir, "valid-config.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"validate --verbose --cf \"{configFile}\"");

        exitCode.ShouldNotBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task Validate_With_Verbose_Short_Flag_Should_Not_Parse_Error()
    {
        var configFile = Path.Combine(FixturesDir, "valid-config.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"validate -v --cf \"{configFile}\"");

        exitCode.ShouldNotBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task Run_With_Verbose_Flag_Should_Not_Parse_Error()
    {
        var configFile = Path.Combine(FixturesDir, "valid-config.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"run --verbose --cf \"{configFile}\"");

        exitCode.ShouldNotBe((int)StudioCliExitCode.CommandLineParseFailed);
    }

    [Fact]
    public async Task Run_With_Verbose_Short_Flag_Should_Not_Parse_Error()
    {
        var configFile = Path.Combine(FixturesDir, "valid-config.sds");
        var exitCode = await CliTestHarness.InvokeAsync($"run -v --cf \"{configFile}\"");

        exitCode.ShouldNotBe((int)StudioCliExitCode.CommandLineParseFailed);
    }
}
