using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Versioning;
using SlnDependencyDiagramGenerator.Exceptions;
using SlnDependencyDiagramGenerator.Parser;
using Shouldly;
using Xunit;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Parser;

public class ProjectAssetReaderFixture
{
    public class IsRequestedDifferentVersion : ProjectAssetReaderFixture
    {
        [Fact]
        public void Should_Return_False_When_No_Requested_Range()
        {
            ProjectAssetReader.IsRequestedDifferentVersion(null, NuGetVersion.Parse("2.1.0")).ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_False_When_No_Resolved_Version()
        {
            ProjectAssetReader.IsRequestedDifferentVersion(CreateMinimumRange(), null).ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_False_When_Exact_Pin_Matches_Resolved_Version()
        {
            ProjectAssetReader.IsRequestedDifferentVersion(CreateExactPin("1.2.3"), NuGetVersion.Parse("1.2.3")).ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_True_When_Exact_Pin_Differs_From_Resolved_Version()
        {
            ProjectAssetReader.IsRequestedDifferentVersion(CreateExactPin("1.2.3"), NuGetVersion.Parse("1.2.4")).ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_False_When_Range_Resolves_Within_Bounds()
        {
            // >= 2.0.0 resolving to 2.1.0 is normal NuGet behaviour — the version satisfies the
            // request, so it must NOT be reported as "requested a different version" (no noise).
            ProjectAssetReader.IsRequestedDifferentVersion(CreateMinimumRange(), NuGetVersion.Parse("2.1.0")).ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_False_When_Range_Resolves_At_Inclusive_Lower_Bound()
        {
            ProjectAssetReader.IsRequestedDifferentVersion(CreateMinimumRange(), NuGetVersion.Parse("2.0.0")).ShouldBeFalse();
        }

        [Fact]
        public void Should_Return_True_When_Range_Resolves_Below_Minimum()
        {
            ProjectAssetReader.IsRequestedDifferentVersion(CreateMinimumRange(), NuGetVersion.Parse("1.9.0")).ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_True_When_Range_Resolves_Outside_Upper_Bound()
        {
            // [2.0.0, 3.0.0) — 3.1.0 is above the exclusive upper bound.
            ProjectAssetReader.IsRequestedDifferentVersion(CreateRange("2.0.0", "3.0.0", includeMax: false), NuGetVersion.Parse("3.1.0")).ShouldBeTrue();
        }

        [Fact]
        public void Should_Return_False_When_Floating_Range_Resolves_In_Bounds()
        {
            ProjectAssetReader.IsRequestedDifferentVersion(VersionRange.Parse("1.*"), NuGetVersion.Parse("1.5.0")).ShouldBeFalse();
        }

        // Equivalent to the shorthand ">= 2.0.0" (minimum bound, inclusive, no maximum).
        private static VersionRange CreateMinimumRange()
            => new(new NuGetVersion("2.0.0"), includeMinVersion: true);

        // An exact pin such as "[1.2.3, 1.2.3]".
        private static VersionRange CreateExactPin(string version)
            => new(new NuGetVersion(version), true, new NuGetVersion(version), true);

        // A bounded range such as "[2.0.0, 3.0.0)" when includeMax is false.
        private static VersionRange CreateRange(string minVersion, string maxVersion, bool includeMax)
            => new(new NuGetVersion(minVersion), true, new NuGetVersion(maxVersion), includeMax);
    }

    public class LoadLockFile : ProjectAssetReaderFixture
    {
        [Fact]
        public void Should_Throw_ProjectAssetsException_When_Assets_File_Is_Missing()
        {
            var reader = new ProjectAssetReader(NullLogger<ProjectAssetReader>.Instance);

            var exception = Should.Throw<ProjectAssetsException>(
                () => reader.GetTargetFrameworks(@"X:\nonexistent\project\project.csproj"));

            exception.Message.ShouldContain("Run 'dotnet restore' before generating diagrams. Missing assets file:");
        }
    }
}
