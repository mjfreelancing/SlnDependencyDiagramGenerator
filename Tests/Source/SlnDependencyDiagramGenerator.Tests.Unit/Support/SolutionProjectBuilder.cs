using SlnDependencyDiagramGenerator.Parser;
using System.Collections.Generic;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Support;

/// <summary>Builds <see cref="SolutionProject"/> instances for summary and renderer unit tests.</summary>
internal sealed class SolutionProjectBuilder
{
    private string _name = "TestProject";
    private string _path = "TestProject.csproj";
    private string[] _targetFrameworks = ["net10.0"];
    private readonly List<ProjectReference> _projectReferences = [];
    private readonly List<FrameworkReference> _frameworkReferences = [];
    private readonly List<PackageReference> _packageReferences = [];

    /// <summary>Sets the project name.</summary>
    /// <param name="name">The project name.</param>
    /// <returns>The builder instance.</returns>
    public SolutionProjectBuilder WithName(string name)
    {
        _name = name;

        return this;
    }

    /// <summary>Sets the project path.</summary>
    /// <param name="path">The project path.</param>
    /// <returns>The builder instance.</returns>
    public SolutionProjectBuilder WithPath(string path)
    {
        _path = path;

        return this;
    }

    /// <summary>Sets target frameworks.</summary>
    /// <param name="targetFrameworks">The target frameworks.</param>
    /// <returns>The builder instance.</returns>
    public SolutionProjectBuilder WithTargetFrameworks(params string[] targetFrameworks)
    {
        _targetFrameworks = targetFrameworks;

        return this;
    }

    /// <summary>Adds a project reference.</summary>
    /// <param name="projectPath">The project path.</param>
    /// <returns>The builder instance.</returns>
    public SolutionProjectBuilder AddProjectReference(string projectPath)
    {
        _projectReferences.Add(new ProjectReference
        {
            Path = projectPath
        });

        return this;
    }

    /// <summary>Adds a framework reference.</summary>
    /// <param name="frameworkName">The framework name.</param>
    /// <returns>The builder instance.</returns>
    public SolutionProjectBuilder AddFrameworkReference(string frameworkName)
    {
        _frameworkReferences.Add(new FrameworkReference
        {
            Name = frameworkName
        });

        return this;
    }

    /// <summary>Adds an explicit package reference with optional transitive children.</summary>
    /// <param name="name">The package name.</param>
    /// <param name="version">The resolved version.</param>
    /// <param name="transitiveReferences">Optional transitive references.</param>
    /// <returns>The builder instance.</returns>
    public SolutionProjectBuilder AddDirectPackage(string name, string version, params PackageReference[] transitiveReferences)
    {
        _packageReferences.Add(new PackageReference(false, 0)
        {
            Name = name,
            Version = version,
            TransitiveReferences = transitiveReferences
        });

        return this;
    }

    /// <summary>Creates a transitive package reference.</summary>
    /// <param name="name">The package name.</param>
    /// <param name="version">The resolved version.</param>
    /// <param name="depth">The transitive depth.</param>
    /// <param name="requestedVersionRange">The requested version range.</param>
    /// <param name="requestedDifferentVersion">Indicates whether a different version was requested.</param>
    /// <param name="transitiveReferences">Optional nested transitive references.</param>
    /// <returns>A transitive package reference.</returns>
    public static PackageReference CreateTransitivePackage(string name, string version, int depth,
        string? requestedVersionRange = null, bool requestedDifferentVersion = false,
        params PackageReference[] transitiveReferences)
    {
        return new PackageReference(true, depth)
        {
            Name = name,
            Version = version,
            RequestedVersionRange = requestedVersionRange,
            RequestedDifferentVersion = requestedDifferentVersion,
            TransitiveReferences = transitiveReferences
        };
    }

    /// <summary>Builds a solution project instance.</summary>
    /// <returns>A populated solution project object.</returns>
    public SolutionProject Build()
    {
        return new SolutionProject
        {
            Name = _name,
            Path = _path,
            TargetFrameworks = _targetFrameworks,
            ProjectReferences = [.. _projectReferences],
            FrameworkReferences = [.. _frameworkReferences],
            PackageReferences = [.. _packageReferences]
        };
    }
}
