namespace SlnDependencyDiagramGenerator.Config;

/// <summary>Specifies diagram options that determine how the diagram will be styled.</summary>
public sealed class GeneratorDiagramOptions
{
    /// <summary>Diagram direction options, using standard flowchart notation.</summary>
    public enum DiagramDirection
    {
        /// <summary>Left to right.</summary>
        LR,

        /// <summary>Right to left.</summary>
        RL,

        /// <summary>Top to bottom.</summary>
        TB,

        /// <summary>Bottom to top.</summary>
        BT
    }

    /// <summary>The fill style used for framework and package dependencies.</summary>
    public sealed class FillStyle
    {
        /// <summary>The CSS or RGB fill color.</summary>
        public string Fill { get; set; }

        /// <summary>The opacity. This should be a value between 0 and 1.</summary>
        public double Opacity { get; set; }
    }

    /// <summary>Specifies grouping behavior and style for diagram containers.</summary>
    public sealed class GroupingOptions
    {
        /// <summary>
        /// Indicates whether project and multi-version package grouping containers are rendered.
        /// When false, all nodes are rendered without group containers.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>The fill style used for grouping container backgrounds.</summary>
        public FillStyle BackgroundStyle { get; init; } = new()
        {
            // Match D2 Neutral Default container tone (B5) so group labels stay readable.
            Fill = "#E7EBFC",
            Opacity = 1
        };
    }

    /// <summary>Specifies the direction the diagram flows. Defaults to <see cref="DiagramDirection.LR"/>.</summary>
    public DiagramDirection Direction { get; set; } = DiagramDirection.LR;

    /// <summary>The fill style to use for framework dependencies referenced by a project.</summary>
    public FillStyle FrameworkStyle { get; init; } = new();

    /// <summary>The fill style to use for explicit package dependencies referenced by a project.</summary>
    public FillStyle PackageStyle { get; init; } = new();

    /// <summary>The fill style to use for transitive (implicit) package dependencies referenced by a project.</summary>
    public FillStyle TransitiveStyle { get; init; } = new();

    /// <summary>The name (title) to use for the group of projects parsed.</summary>
    public string GroupName { get; set; }

    /// <summary>The alias used to represent the project group in generated diagram files.
    /// This prefix is not visible in diagram image output, but is required to visually
    /// group projects together in D2 and Mermaid output.</summary>
    public string GroupNameAlias { get; set; }

    /// <summary>Grouping behavior and style options applied across all diagram formats.</summary>
    public GroupingOptions Grouping { get; init; } = new();

    /// <summary>The diagram format(s) to generate. Must contain at least one format.</summary>
    public DiagramFormat[] Formats { get; set; } = [];
}
