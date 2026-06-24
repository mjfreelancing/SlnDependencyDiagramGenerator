using SlnDependencyDiagramGenerator.Config;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Support;

/// <summary>Builds <see cref="DependencyGeneratorConfig"/> instances for tests.</summary>
internal sealed class TestConfigBuilder
{
    private string _solutionPath = "test.sln";
    private string[] _regexToInclude = ["\\\\.*\\.csproj"];
    private string[] _regexToExclude = [];
    private string[] _packagesToExclude = [];
    private string[] _frameworksToExclude = [];

    private bool _individualEnabled = true;
    private bool _individualIncludeDependencies = true;
    private int _individualTransitiveDepth = 1;

    private bool _allEnabled = true;
    private bool _allIncludeDependencies = true;
    private int _allTransitiveDepth = 1;

    private GeneratorDiagramOptions.DiagramDirection _diagramDirection = GeneratorDiagramOptions.DiagramDirection.LR;
    private string _groupName = "Test Group";
    private string _groupNameAlias = "test";
    private bool _groupingEnabled = false;

    private string _frameworkFill = "#ECCBC0";
    private double _frameworkOpacity = 0.8;
    private string _packageFill = "#ADD8E6";
    private double _packageOpacity = 0.8;
    private string _transitiveFill = "#FFEC96";
    private double _transitiveOpacity = 0.8;
    private string _groupBackgroundFill = "#E7EBFC";
    private double _groupBackgroundOpacity = 1.0;

    private DiagramFormat[] _formats = [DiagramFormat.D2];

    private bool _clearContents = false;
    private string _rootPath = "output";
    private DiagramImageFormat[] _imageFormats = [];

    /// <summary>Sets the solution path.</summary>
    /// <param name="solutionPath">The solution path.</param>
    /// <returns>The builder instance.</returns>
    public TestConfigBuilder WithSolutionPath(string solutionPath)
    {
        _solutionPath = solutionPath;

        return this;
    }

    /// <summary>Sets include regex patterns.</summary>
    /// <param name="regexToInclude">The regex include patterns.</param>
    /// <returns>The builder instance.</returns>
    public TestConfigBuilder WithRegexToInclude(params string[] regexToInclude)
    {
        _regexToInclude = regexToInclude;

        return this;
    }

    /// <summary>Sets exclude regex patterns.</summary>
    /// <param name="regexToExclude">The regex exclude patterns.</param>
    /// <returns>The builder instance.</returns>
    public TestConfigBuilder WithRegexToExclude(params string[] regexToExclude)
    {
        _regexToExclude = regexToExclude;

        return this;
    }

    /// <summary>Sets package exclusions.</summary>
    /// <param name="packagesToExclude">Package IDs to exclude.</param>
    /// <returns>The builder instance.</returns>
    public TestConfigBuilder WithPackagesToExclude(params string[] packagesToExclude)
    {
        _packagesToExclude = packagesToExclude;

        return this;
    }

    /// <summary>Sets framework exclusions.</summary>
    /// <param name="frameworksToExclude">Framework IDs to exclude.</param>
    /// <returns>The builder instance.</returns>
    public TestConfigBuilder WithFrameworksToExclude(params string[] frameworksToExclude)
    {
        _frameworksToExclude = frameworksToExclude;

        return this;
    }

    /// <summary>Sets diagram formats.</summary>
    /// <param name="formats">Diagram formats.</param>
    /// <returns>The builder instance.</returns>
    public TestConfigBuilder WithFormats(params DiagramFormat[] formats)
    {
        _formats = formats;

        return this;
    }

    /// <summary>Builds a test configuration instance.</summary>
    /// <returns>A populated configuration object.</returns>
    public DependencyGeneratorConfig Build()
    {
        return new DependencyGeneratorConfig
        {
            Projects = new GeneratorProjectOptions
            {
                SolutionPath = _solutionPath,
                RegexToInclude = _regexToInclude,
                RegexToExclude = _regexToExclude,
                PackagesToExclude = _packagesToExclude,
                FrameworksToExclude = _frameworksToExclude,
                Individual = new GeneratorProjectOptions.ProjectScope
                {
                    Enabled = _individualEnabled,
                    IncludeDependencies = _individualIncludeDependencies,
                    TransitiveDepth = _individualTransitiveDepth
                },
                All = new GeneratorProjectOptions.ProjectScope
                {
                    Enabled = _allEnabled,
                    IncludeDependencies = _allIncludeDependencies,
                    TransitiveDepth = _allTransitiveDepth
                }
            },
            Diagram = new GeneratorDiagramOptions
            {
                Direction = _diagramDirection,
                GroupName = _groupName,
                GroupNameAlias = _groupNameAlias,
                FrameworkStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = _frameworkFill,
                    Opacity = _frameworkOpacity
                },
                PackageStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = _packageFill,
                    Opacity = _packageOpacity
                },
                TransitiveStyle = new GeneratorDiagramOptions.FillStyle
                {
                    Fill = _transitiveFill,
                    Opacity = _transitiveOpacity
                },
                Grouping = new GeneratorDiagramOptions.GroupingOptions
                {
                    Enabled = _groupingEnabled,
                    BackgroundStyle = new GeneratorDiagramOptions.FillStyle
                    {
                        Fill = _groupBackgroundFill,
                        Opacity = _groupBackgroundOpacity
                    }
                },
                Formats = _formats
            },
            Export = new GeneratorExportOptions
            {
                ClearContents = _clearContents,
                RootPath = _rootPath,
                ImageFormats = _imageFormats
            }
        };
    }
}
