using SlnDependencyDiagramGenerator.Tests.Integration.Support;

namespace SlnDependencyDiagramGenerator.Tests.Integration;

public class SnapshotApprovalScenariosFixture : FixtureCollectionTestBase
{
    public class OutputSnapshots : SnapshotApprovalScenariosFixture
    {
        [Fact]
        public async Task Should_Emit_Received_Snapshot_For_Basic_AllFormats()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Basic", "Basic Group", "basic");

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var snapshot = IntegrationTestHarness.CollectExportSnapshot(scenarioRun.ExportRoot);

            await Verifier.Verify(snapshot);
        }

        [Fact]
        public async Task Should_Emit_Received_Snapshot_For_Conflicts_AllFormats()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("Conflicts", "Conflicts Group", "conflicts");

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var snapshot = IntegrationTestHarness.CollectExportSnapshot(scenarioRun.ExportRoot);

            await Verifier.Verify(snapshot);
        }

        [Fact]
        public async Task Should_Emit_Received_Snapshot_For_OutOfRange_AllFormats()
        {
            var options = IntegrationTestHarness.CreateScenarioOptions("OutOfRange", "Out Of Range Group", "outofrange");

            using var scenarioRun = await IntegrationTestHarness.RunGeneratorAsync(options);

            var snapshot = IntegrationTestHarness.CollectExportSnapshot(scenarioRun.ExportRoot);

            await Verifier.Verify(snapshot);
        }
    }
}