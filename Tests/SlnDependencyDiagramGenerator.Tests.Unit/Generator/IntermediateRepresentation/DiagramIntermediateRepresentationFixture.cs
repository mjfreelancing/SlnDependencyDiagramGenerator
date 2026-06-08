using System;
using SlnDependencyDiagramGenerator.Generator.IntermediateRepresentation;
using Shouldly;

namespace SlnDependencyDiagramGenerator.Tests.Unit.Generator.IntermediateRepresentation;

public class DiagramIntermediateRepresentationFixture
{
    public class Nodes : DiagramIntermediateRepresentationFixture
    {
        [Fact]
        public void Should_Return_Nodes_In_Insertion_Order_And_Deduplicate_By_Alias()
        {
            var model = new DiagramIntermediateRepresentation();

            model.AddNode("node-a", "Node A", "1.0.0");
            model.AddNode("node-b", "Node B", "2.0.0");
            model.AddNode("node-a", "Node A Updated", "9.9.9");

            model.Nodes.Count.ShouldBe(2);
            model.Nodes[0].Alias.ShouldBe("node-a");
            model.Nodes[0].Label.ShouldBe("Node A");
            model.Nodes[0].Version.ShouldBe("1.0.0");
            model.Nodes[1].Alias.ShouldBe("node-b");
            model.Nodes[1].Label.ShouldBe("Node B");
        }
    }

    public class Edges : DiagramIntermediateRepresentationFixture
    {
        [Fact]
        public void Should_Return_Edges_In_Insertion_Order_And_Deduplicate_By_Endpoints()
        {
            var model = new DiagramIntermediateRepresentation();

            model.AddEdge("a", "b");
            model.AddEdge("a", "c");
            model.AddEdge("a", "b");

            model.Edges.Count.ShouldBe(2);
            model.Edges[0].FromAlias.ShouldBe("a");
            model.Edges[0].ToAlias.ShouldBe("b");
            model.Edges[1].FromAlias.ShouldBe("a");
            model.Edges[1].ToAlias.ShouldBe("c");
        }
    }

    public class Styles : DiagramIntermediateRepresentationFixture
    {
        [Fact]
        public void Should_Return_Styles_In_Insertion_Order_And_Allow_Override_When_Requested()
        {
            var model = new DiagramIntermediateRepresentation();

            model.SetStyleRole("node-a", DiagramIrStyleRole.PackageExplicit, allowOverride: false);
            model.SetStyleRole("node-a", DiagramIrStyleRole.PackageTransitive, allowOverride: false);
            model.SetStyleRole("node-a", DiagramIrStyleRole.Framework, allowOverride: true);
            model.SetStyleRole("node-b", DiagramIrStyleRole.PackageTransitive, allowOverride: false);

            model.Styles.Count.ShouldBe(2);
            model.Styles[0].Alias.ShouldBe("node-a");
            model.Styles[0].Role.ShouldBe(DiagramIrStyleRole.Framework);
            model.Styles[1].Alias.ShouldBe("node-b");
            model.Styles[1].Role.ShouldBe(DiagramIrStyleRole.PackageTransitive);
        }
    }

    public class Groups : DiagramIntermediateRepresentationFixture
    {
        [Fact]
        public void Should_Return_Groups_In_Insertion_Order_And_Track_Node_Membership()
        {
            var model = new DiagramIntermediateRepresentation();

            model.EnsureGroup("group-a", "Group A");
            model.EnsureGroup("group-b", "Group B");
            model.EnsureGroup("group-a", "Group A Updated");

            model.AddNodeToGroup("group-a", "node-a");
            model.AddNodeToGroup("group-a", "node-a");
            model.AddNodeToGroup("group-b", "node-b");

            model.Groups.Count.ShouldBe(2);
            model.Groups[0].Alias.ShouldBe("group-a");
            model.Groups[0].Label.ShouldBe("Group A");
            model.Groups[0].NodeAliases.ShouldBe(["node-a"]);
            model.Groups[1].Alias.ShouldBe("group-b");
            model.Groups[1].NodeAliases.ShouldBe(["node-b"]);

            model.GetNodeGroupAlias("node-a").ShouldBe("group-a");
            model.GetNodeGroupAlias("node-b").ShouldBe("group-b");
            model.GetNodeGroupAlias("node-c").ShouldBeNull();
        }

        [Fact]
        public void Should_Throw_When_Adding_A_Node_To_A_Missing_Group()
        {
            var model = new DiagramIntermediateRepresentation();

            Should.Throw<InvalidOperationException>(() => model.AddNodeToGroup("missing-group", "node-a"))
                .Message.ShouldContain("Group 'missing-group' was not created before assigning node 'node-a'.");
        }
    }
}