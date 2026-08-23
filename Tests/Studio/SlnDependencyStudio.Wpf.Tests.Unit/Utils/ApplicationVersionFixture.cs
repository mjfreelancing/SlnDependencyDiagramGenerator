using Shouldly;
using SlnDependencyStudio.Wpf.Utils;
using System.Reflection;

namespace SlnDependencyStudio.Wpf.Tests.Unit.Utils;

public class ApplicationVersionFixture
{
    public class Value : ApplicationVersionFixture
    {
        [Fact]
        public void Should_Return_The_Assembly_Informational_Version_Without_Source_Revision_Suffix()
        {
            var informationalVersion = typeof(ApplicationVersion).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            informationalVersion.ShouldNotBeNull();

            // The informational version can carry a "+suffix" (SourceRevisionId). The helper must
            // return only the portion before it (e.g. "1.0.0-rc1+<guid>" -> "1.0.0-rc1").
            var expected = informationalVersion.Split('+')[0];

            ApplicationVersion.Value.ShouldBe(expected);
        }
    }
}
