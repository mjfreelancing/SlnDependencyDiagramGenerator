using Shouldly;
using SlnDependencyStudio.Shared.Utils;

namespace SlnDependencyStudio.Shared.Tests.Unit.Utils;

public class CommandLineUtilsFixture
{
    [Fact]
    public void Should_Split_Simple_Arguments_On_Whitespace()
    {
        CommandLineUtils.SplitArguments("--config Release --no-build")
            .ShouldBe(new[] { "--config", "Release", "--no-build" });
    }

    [Fact]
    public void Should_Preserve_Quoted_Value_With_Spaces_As_Single_Argument()
    {
        CommandLineUtils.SplitArguments("--file \"my file.txt\"")
            .ShouldBe(new[] { "--file", "my file.txt" });
    }

    [Fact]
    public void Should_Strip_Quotes_From_Sole_Quoted_Argument()
    {
        CommandLineUtils.SplitArguments("\"my file.txt\"")
            .ShouldBe(new[] { "my file.txt" });
    }

    [Fact]
    public void Should_Support_Escaped_Quotes_Inside_Quoted_Value()
    {
        CommandLineUtils.SplitArguments("--message \"say \\\"hi\\\"\"")
            .ShouldBe(new[] { "--message", "say \"hi\"" });
    }

    [Fact]
    public void Should_Tolerate_Unterminated_Quote_As_Single_Argument()
    {
        CommandLineUtils.SplitArguments("--file \"my file.txt")
            .ShouldBe(new[] { "--file", "my file.txt" });
    }

    [Fact]
    public void Should_Return_Empty_For_Empty_Or_Whitespace_Input()
    {
        CommandLineUtils.SplitArguments(string.Empty).ShouldBeEmpty();
        CommandLineUtils.SplitArguments("   ").ShouldBeEmpty();
    }
}
