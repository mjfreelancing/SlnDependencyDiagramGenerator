# Testing Plan — SlnDependencyDiagramGenerator

**Date:** May 2026
**Status:** Draft — for review and refinement before implementation
**Scope:** Unit tests, integration/snapshot tests (Verify library)

---

## 1. Goals

- Establish confidence that the parser, graph model, renderers, summary generator, and validators behave correctly across all supported scenarios.
- Use snapshot (golden-file) tests via the [Verify](https://github.com/VerifyTests/Verify) library to lock in rendered diagram text and summary Markdown output, so any accidental regression in output format or content is caught automatically.
- Keep the test fixture solutions minimal and self-contained so tests run without needing the full `SlnDependencyDiagramGenerator` solution built or restored.
- Separate unit tests (no I/O, no MSBuild, no NuGet) from integration tests (real on-disk fixture solutions, real assets files, real renderer output) so the unit layer runs fast and the integration layer provides end-to-end confidence.

---

## 2. Test Project Structure

```
Tests/
  SlnDependencyDiagramGenerator.Tests.Unit/
    SlnDependencyDiagramGenerator.Tests.Unit.csproj
    Validators/
    Summary/
    Renderers/
    Support/

  SlnDependencyDiagramGenerator.Tests.Integration/
    SlnDependencyDiagramGenerator.Tests.Integration.csproj
    Fixtures/                          ← fake solutions / projects used as test inputs
      ...
    Scenarios/                         ← one folder per integration test scenario
    Snapshots/                         ← Verify golden files (*.verified.txt / *.verified.md)
    Support/
```

Both projects live in a `Tests/` folder at the solution root and are added to the `.sln`.

---

## 3. Unit Tests

### 3.1 Scope

Unit tests cover pure logic that has no external I/O dependencies: validators, the summary generator (given pre-built `SolutionProject` object graphs), the target-framework badge provider, the intermediate representation builder, and individual renderer serialization (given a pre-built IR).

The `SolutionParser` and `ProjectAssetReader` are inherently I/O-bound (MSBuild, NuGet lock files) and are covered by integration tests only.

Unit test tooling guidance:

- Use Shouldly for assertions.
- Use NSubstitute when fakes or substitutes are required.

---

### 3.2 Validator Tests

Each validator is tested independently with all meaningful valid and invalid input combinations.

**`DependencyGeneratorConfigValidator`**

| Test               | Expected         |
| ------------------ | ---------------- |
| Fully valid config | No errors        |
| `Projects` is null | Validation error |
| `Diagram` is null  | Validation error |
| `Export` is null   | Validation error |

**`GeneratorProjectOptionsValidator`**

| Test                                         | Expected                               |
| -------------------------------------------- | -------------------------------------- |
| Valid solution path (existing file)          | No errors                              |
| Valid solution path with `.slnx` extension   | No errors                              |
| `SolutionPath` uses unsupported extension    | Error (`.sln` or `.slnx` required)     |
| `SolutionPath` is null                       | Error                                  |
| `SolutionPath` is empty string               | Error                                  |
| `SolutionPath` points to a non-existent file | Error with message containing the path |
| `RegexToInclude` is null                     | Error                                  |
| `RegexToInclude` is empty array              | Error                                  |
| `RegexToExclude` is null                     | Error                                  |
| `PackagesToExclude` is null                  | Error                                  |
| `FrameworksToExclude` is null                | Error                                  |
| `Individual.TransitiveDepth` is -1           | Error                                  |
| `Individual.TransitiveDepth` is 0            | No errors                              |
| `All.TransitiveDepth` is -1                  | Error                                  |
| `Individual` is null                         | Error                                  |
| `All` is null                                | Error                                  |

**`GeneratorDiagramOptionsValidator`**

| Test                                | Expected                                              |
| ----------------------------------- | ----------------------------------------------------- |
| Valid options with `d2` format      | No errors                                             |
| Valid options with `mermaid` format | No errors                                             |
| Valid options with both formats     | No errors                                             |
| `Formats` is null                   | Error                                                 |
| `Formats` is empty                  | Error                                                 |
| `FrameworkStyle` is null            | Error                                                 |
| `PackageStyle` is null              | Error                                                 |
| `TransitiveStyle` is null           | Error                                                 |
| `Grouping` is null                  | Error                                                 |
| `Grouping.BackgroundStyle` is null  | Error                                                 |
| `Direction` has invalid enum value  | Error                                                 |
| `GroupName` is null or empty        | Error                                                 |
| `GroupNameAlias` is null or empty   | Error                                                 |
| Style `Fill` is null or empty       | Error                                                 |
| Style `Opacity` out of range        | Error (if validation exists; flag for implementation) |

**`GeneratorExportOptionsValidator`**

| Test                                       | Expected  |
| ------------------------------------------ | --------- |
| Valid path and image formats               | No errors |
| `RootPath` is null or empty                | Error     |
| `ImageFormats` is null                     | Error     |
| `ImageFormats` contains invalid enum value | Error     |

---

### 3.3 `TargetFrameworkBadgeProvider` Tests

| Test                                                                             | Expected                                            |
| -------------------------------------------------------------------------------- | --------------------------------------------------- |
| `net8.0` → badge contains `.NET-8.0`                                             | Correct URL segment                                 |
| `net9.0` → badge contains `.NET-9.0`                                             | Correct URL segment                                 |
| `net10.0` → badge contains `.NET-10.0`                                           | Correct URL segment                                 |
| `net10.0-windows10.0.19041` → badge uses base moniker and strips platform suffix | Badge uses `10.0` moniker                           |
| `net8.0` and `net9.0` get different colors                                       | Distinct hex color per TFM                          |
| Same TFM called twice returns cached badge                                       | Same string identity, no duplicate color assignment |
| `netstandard2.1` parses version correctly                                        | Correct URL segment                                 |

---

### 3.4 `SummaryDependencyGenerator` Tests

These tests construct `IDictionary<string, SolutionProject>` in memory and assert the markdown output using Verify snapshots. Scenarios:

| Scenario                                                             | Key details                                                      |
| -------------------------------------------------------------------- | ---------------------------------------------------------------- |
| Single project, no dependencies                                      | `### Dependencies` section shows `* None`                        |
| Single project, one framework reference                              | Framework reference appears in dependency list                   |
| Single project, framework reference excluded at parse time           | Does not appear (pre-filtered before summary)                    |
| Single project, one direct package                                   | Package appears with `v{version}`                                |
| Single project, direct + transitive packages                         | Both appear; transitive in correct section                       |
| Two projects, no cross-reference                                     | Each section independent; no conflict table                      |
| Two projects, project A references project B                         | B appears in A's dependency list; A references B via project ref |
| Two projects, same package same version                              | No conflict table                                                |
| Two projects, same package different versions                        | Conflict table present with correct rows                         |
| Conflict: package requested a different version via transitive chain | Conflict details cell shows full resolution path                 |
| Project with multiple target frameworks                              | Multiple badges in project section                               |
| Project with platform-specific TFM (`net10.0-windows`)               | Badge strips platform suffix; base moniker displayed             |
| Multiple projects ordered alphabetically                             | Sections appear in alphabetical project order                    |
| Circular project reference                                           | `DependencyGeneratorException` thrown                            |
| Project reference not found in solution projects                     | `DependencyGeneratorException` thrown                            |

---

### 3.5 `DiagramIntermediateRepresentation` Tests

Test the IR builder directly (no renderer output needed).

| Scenario                              | Assertion                                   |
| ------------------------------------- | ------------------------------------------- |
| Add node, retrieve in insertion order | `Nodes` contains node in order              |
| Add duplicate node alias              | Second add is ignored (de-duplication)      |
| Add edge A→B, add edge A→B again      | Second edge ignored (de-duplication)        |
| Add group, assign nodes to group      | Group membership reflected in `NodeToGroup` |
| Styles attached to correct alias      | Style role reachable from alias             |

---

### 3.6 Renderer Serialization Tests (Unit, given a pre-built IR)

Construct small fixed `DependencyGraphModel` objects and snapshot-test the serialized text from each renderer without running external tools.

**Common model shapes:**

- One project, one direct package, grouping disabled
- One project, one direct package, one transitive package, grouping disabled
- One project, one framework reference, grouping disabled
- Two projects, project reference, grouping disabled
- Two projects, shared package with multiple versions (group node), grouping enabled
- One project, all three dependency types (framework, direct package, transitive), grouping enabled
- `direction: LR` vs `direction: TB` produces correct direction token

These same model shapes are rendered through both `D2DiagramRenderer` and `MermaidDiagramRenderer` so snapshot files cover both formats side-by-side.

---

## 4. Integration Tests

### 4.1 Philosophy

Integration tests exercise the full pipeline from a real on-disk fixture solution through to rendered output text (not image files — that requires external CLI tools). The Verify library stores approved `.verified.txt` / `.verified.md` files next to the test. On first run, Verify creates received files for manual review; once approved they become the regression baseline.

Image export (png, svg, pdf) is explicitly out of scope for automated tests because it requires `d2` and `mmdc` CLI tools to be on PATH and is environment-dependent.

Each scenario folder under `Fixtures/` is a minimal self-contained solution. Each fixture keeps both `.sln` and `.slnx` files that reference the same projects so parser scenarios can assert format parity without duplicating project content. After any change to the fixture projects, `dotnet restore` must be run against the fixture solution to regenerate `project.assets.json` before test runs. The fixture solutions should be committed with their `obj/project.assets.json` files so CI can run without a restore step.

Malformed-solution parser tests use dedicated static input files (for both `.sln` and `.slnx`) under a test-data folder, not full fixture projects.

---

### 4.2 Fixture Solution Design

All fixture projects live under `Tests/SlnDependencyDiagramGenerator.Tests.Integration/Fixtures/`.

#### Fixture Solution: `Basic`

A single solution containing the following projects, each targeting one or more frameworks.

**Projects:**

| Project      | Type              | Target frameworks             | Notes                                                                      |
| ------------ | ----------------- | ----------------------------- | -------------------------------------------------------------------------- |
| `LibA`       | SDK class library | `net8.0`, `net9.0`, `net10.0` | Has one direct NuGet package (e.g. `Newtonsoft.Json 13.0.3`)               |
| `LibB`       | SDK class library | `net8.0`, `net9.0`, `net10.0` | References `LibA`; has one different direct package (e.g. `Serilog 3.1.1`) |
| `AppConsole` | SDK console app   | `net10.0`                     | References both `LibA` and `LibB`; adds one more direct package            |

This covers: multi-TFM discovery, project-to-project references, direct packages, no conflicts.

---

#### Fixture Solution: `Transitive`

Designed to produce non-trivial transitive depth.

**Projects:**

| Project      | Type              | Target frameworks   | Notes                                                                                                                                                                |
| ------------ | ----------------- | ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `CoreLib`    | SDK class library | `net9.0`, `net10.0` | Explicit package that itself brings transitive packages (e.g. `FluentValidation` which has no transitive deps — pick a package that has at least one transitive dep) |
| `AppConsole` | SDK console app   | `net10.0`           | References `CoreLib`                                                                                                                                                 |

Tests transitive depth 0, 1, and 2 configuration options against the same fixture.

---

#### Fixture Solution: `Conflicts`

Designed to produce cross-project NuGet version conflicts and verify the conflict table in the summary.

**Projects:**

| Project | Type              | Target frameworks | Notes                               |
| ------- | ----------------- | ----------------- | ----------------------------------- |
| `LibV1` | SDK class library | `net10.0`         | References `Newtonsoft.Json 12.0.3` |
| `LibV2` | SDK class library | `net10.0`         | References `Newtonsoft.Json 13.0.3` |
| `App`   | SDK console app   | `net10.0`         | References both `LibV1` and `LibV2` |

Produces a conflict table in the summary. Also useful to confirm the conflict rows and resolution-path details snapshot correctly.

---

#### Fixture Solution: `FrameworkRefs`

Designed to exercise framework reference handling and `frameworksToExclude`.

**Projects:**

| Project   | Type              | Target frameworks   | Notes                                                                                                               |
| --------- | ----------------- | ------------------- | ------------------------------------------------------------------------------------------------------------------- |
| `WebLib`  | SDK class library | `net10.0`           | Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in addition to the implicit `Microsoft.NETCore.App` |
| `CoreLib` | SDK class library | `net9.0`, `net10.0` | No explicit framework references (gets `Microsoft.NETCore.App` implicitly)                                          |

Tests: both framework refs visible when `frameworksToExclude` is empty; `Microsoft.NETCore.App` disappears when excluded; custom framework reference (`Microsoft.AspNetCore.App`) excluded individually; all framework refs excluded together.

---

#### Fixture Solution: `Exclusions`

Designed to validate `packagesToExclude` and `regexToExclude`.

**Projects:**

| Project       | Type              | Target frameworks | Notes                                       |
| ------------- | ----------------- | ----------------- | ------------------------------------------- |
| `LibA`        | SDK class library | `net10.0`         | References packages P1 and P2               |
| `LibB`        | SDK class library | `net10.0`         | References package P1 and project `LibA`    |
| `LibExcluded` | SDK class library | `net10.0`         | Intentionally excluded via `regexToExclude` |

Tests: P1 excluded removes it from both projects and strips its transitive chain; P2 excluded only affects `LibA`; `LibExcluded` absent from all output; `regexToInclude` covering only `LibA` and `LibB` leaves `LibExcluded` out.

---

#### Fixture Solution: `Grouping`

Focused purely on grouping behavior differences between the two renderers.

**Projects:**

| Project | Type              | Target frameworks | Notes                                                              |
| ------- | ----------------- | ----------------- | ------------------------------------------------------------------ |
| `LibA`  | SDK class library | `net10.0`         | References package P (version 1.0.0)                               |
| `LibB`  | SDK class library | `net10.0`         | References package P (version 2.0.0) — creates multi-version group |

Tests: grouping enabled → D2 group containers and Mermaid subgraphs present; grouping disabled → no group syntax in either format; multi-version package gets a group node in both renderers; group background style applied.

---

#### Fixture Solution: `SingleFramework`

A single-target-framework solution to test that single-TFM discovery and output folder structure work correctly — no multi-framework loop, no per-TFM folder confusion.

---

### 4.3 Integration Test Scenarios

Each scenario is a separate test method, lives in its own class under `Scenarios/`, and uses Verify for output comparison. The scenario name becomes part of the Verify snapshot filename so snapshots are organized by scenario.

---

#### Scenario group: Summary output (`SummaryScenarios`)

| Test                                 | Fixture         | Config                                           | Snapshot                                                                        |
| ------------------------------------ | --------------- | ------------------------------------------------ | ------------------------------------------------------------------------------- |
| Summary_Basic_AllFrameworks          | `Basic`         | All frameworks, no exclusions                    | `Dependency Summary.md` for net8.0, net9.0, net10.0                             |
| Summary_Basic_Net10Only              | `Basic`         | Filtered to net10.0 only                         | `Dependency Summary.md` for net10.0                                             |
| Summary_Conflicts_Net10              | `Conflicts`     | net10.0, no exclusions                           | Conflict table present with correct package, versions, and project names        |
| Summary_FrameworkRefs_NoExclusions   | `FrameworkRefs` | No exclusions                                    | Both `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` appear              |
| Summary_FrameworkRefs_ExcludeNETCore | `FrameworkRefs` | `frameworksToExclude: ["Microsoft.NETCore.App"]` | Only `Microsoft.AspNetCore.App` in `WebLib`; `CoreLib` has no framework refs    |
| Summary_FrameworkRefs_ExcludeAll     | `FrameworkRefs` | Exclude both framework refs                      | No framework refs in any project                                                |
| Summary_Exclusions_PackageP1         | `Exclusions`    | `packagesToExclude: ["P1"]`                      | P1 absent from both projects                                                    |
| Summary_Exclusions_RegexExclude      | `Exclusions`    | `regexToExclude` matching `LibExcluded`          | `LibExcluded` absent from summary entirely                                      |
| Summary_Transitive_Depth0            | `Transitive`    | `transitiveDepth: 0`                             | No transitive packages in dependency list                                       |
| Summary_Transitive_Depth1            | `Transitive`    | `transitiveDepth: 1`                             | One level of transitive packages                                                |
| Summary_Transitive_Depth2            | `Transitive`    | `transitiveDepth: 2`                             | Two levels of transitive packages (if the fixture package chain is deep enough) |

---

#### Scenario group: D2 diagram output (`D2Scenarios`)

| Test                                 | Fixture         | Config                                           | What is snapshotted                       |
| ------------------------------------ | --------------- | ------------------------------------------------ | ----------------------------------------- |
| D2_Basic_Individual_GroupingDisabled | `Basic`         | `individual.enabled`, grouping off, direction LR | Per-project `.d2` files for each TFM      |
| D2_Basic_All_GroupingDisabled        | `Basic`         | `all.enabled`, grouping off, direction LR        | All-projects `.d2` for each TFM           |
| D2_Basic_All_GroupingEnabled         | `Basic`         | `all.enabled`, grouping on                       | Group containers in `.d2` output          |
| D2_Conflicts_GroupNode               | `Grouping`      | Grouping enabled                                 | Multi-version package group node in `.d2` |
| D2_Conflicts_NoGroupNode             | `Grouping`      | Grouping disabled                                | Flat nodes, no group                      |
| D2_Direction_TB                      | `Basic`         | `direction: TB`                                  | `direction: down` in `.d2` content        |
| D2_FrameworkStyle                    | `FrameworkRefs` | Custom framework fill color                      | Style block contains expected hex color   |
| D2_PackageStyle                      | `Basic`         | Custom package fill color                        | Style block contains expected hex color   |
| D2_TransitiveStyle                   | `Transitive`    | Custom transitive fill color                     | Style block contains expected hex color   |
| D2_TransitiveDepth0                  | `Transitive`    | `transitiveDepth: 0`                             | No transitive nodes in `.d2`              |
| D2_TransitiveDepth2                  | `Transitive`    | `transitiveDepth: 2`                             | Transitive nodes at depth 1 and 2 present |

---

#### Scenario group: Mermaid diagram output (`MermaidScenarios`)

Mirror of the D2 scenarios, run against the same fixture configurations:

| Test                                      | Fixture         | Config                             | What is snapshotted                  |
| ----------------------------------------- | --------------- | ---------------------------------- | ------------------------------------ |
| Mermaid_Basic_Individual_GroupingDisabled | `Basic`         | `individual.enabled`, grouping off | Per-project `.mmd` files             |
| Mermaid_Basic_All_GroupingDisabled        | `Basic`         | `all.enabled`, grouping off        | All-projects `.mmd`                  |
| Mermaid_Basic_All_GroupingEnabled         | `Basic`         | `all.enabled`, grouping on         | `subgraph` blocks in `.mmd`          |
| Mermaid_Conflicts_GroupNode               | `Grouping`      | Grouping enabled                   | Multi-version package node in `.mmd` |
| Mermaid_Direction_TB                      | `Basic`         | `direction: TB`                    | `TD` direction token in `.mmd`       |
| Mermaid_FrameworkStyle                    | `FrameworkRefs` | Custom framework fill              | `style` line contains hex color      |
| Mermaid_TransitiveDepth0                  | `Transitive`    | `transitiveDepth: 0`               | No transitive nodes                  |
| Mermaid_TransitiveDepth2                  | `Transitive`    | `transitiveDepth: 2`               | Transitive nodes present             |

---

#### Scenario group: Parser / discovery (`ParserScenarios`)

These test `SolutionParser` directly rather than via `DependencyGenerator`.

| Test                            | Fixture           | Assertion                                                          |
| ------------------------------- | ----------------- | ------------------------------------------------------------------ |
| DiscoverFrameworks_Basic        | `Basic`           | Returns `["net8.0", "net9.0", "net10.0"]` ordered ascending        |
| DiscoverFrameworks_Basic_Slnx   | `Basic` (`.slnx`) | Same result as `.sln` path for the same fixture                    |
| DiscoverFrameworks_SingleTfm    | `SingleFramework` | Returns `["net10.0"]`                                              |
| DiscoverFrameworks_RegexExclude | `Exclusions`      | `LibExcluded` project is not contributing frameworks when excluded |
| Parse_Basic_Net10               | `Basic`           | Returns expected project names, references, packages for net10.0   |
| Parse_Basic_Net10_Slnx          | `Basic` (`.slnx`) | Equivalent project/package/framework output to `.sln` path         |
| Parse_FrameworkRef_Present      | `FrameworkRefs`   | `Microsoft.AspNetCore.App` in `FrameworkReferences`                |
| Parse_FrameworkRef_Excluded     | `FrameworkRefs`   | `Microsoft.NETCore.App` absent when excluded                       |
| Parse_Package_ExplicitDepth     | `Transitive`      | Explicit packages at depth 0, transitive at depth 1+               |
| Parse_Package_ExcludedPackage   | `Exclusions`      | Excluded package absent from `PackageReferences`                   |
| Parse_ProjectReference_Chain    | `Basic`           | `AppConsole` has project refs to `LibA` and `LibB`                 |
| Parse_MalformedSolution_Sln     | Malformed input   | Fails fast with clear parse error containing path and reason       |
| Parse_MalformedSolution_Slnx    | Malformed input   | Fails fast with clear parse error containing path and reason       |
| TfmSortOrder                    | N/A (unit)        | `net8.0` < `net9.0` < `net10.0` after sort                         |

---

#### Scenario group: End-to-end output folder structure (`FolderStructureScenarios`)

These verify that the export folder structure is correct — not the file content, just that the right files exist in the right sub-folders.

| Test                               | Fixture | Config                           | Assertion                                                                     |
| ---------------------------------- | ------- | -------------------------------- | ----------------------------------------------------------------------------- |
| OutputFolders_PerTfmFolderCreated  | `Basic` | d2 + mermaid                     | net8.0/, net9.0/, net10.0/ created under root                                 |
| OutputFolders_D2SubfolderCreated   | `Basic` | d2 only                          | `d2/` sub-folder under each TFM                                               |
| OutputFolders_MmdSubfolderCreated  | `Basic` | mermaid only                     | `mmd/` sub-folder under each TFM                                              |
| OutputFolders_SummaryAlwaysPresent | `Basic` | d2 only                          | `Dependency Summary.md` present even when mermaid not configured              |
| OutputFolders_ClearContents        | `Basic` | `clearContents: true`, run twice | Second run produces only files from second run, not leftover files from first |

---

## 5. Verify Library Configuration

- Use `VerifyTests/Verify` for xUnit.
- Configure Verify using a `ModuleInitializer` in each test project (standard approach for this repo).
- Snapshot files stored in `Snapshots/` alongside the test class file (`UseProjectRelativeDirectory("Snapshots")`).
- Use `VerifySettings` to scrub or normalize any build-time paths that appear in output (e.g. replace fixture absolute paths with a placeholder like `{FixturePath}`) so snapshots are portable between machines.
- Disable automatic diff launch in CI (`Environment.GetEnvironmentVariable("CI")` check or `AutoVerify` only for explicit approval runs).
- Integration tests still write real output files to the configured export folder. The tests then read those files and aggregate them into a `Dictionary<string, string>` where key = logical output path (`{tfm}/{filename}`) and value = file content.
- Each multi-file scenario (e.g. one `.d2` per project per TFM) should use that dictionary in a single Verify call so the snapshot is one coherent approval artifact.

---

## 6. Test Infrastructure / Support

- **Fixture builder helper** — a `FixtureLocator` static class that resolves fixture solution paths relative to the test assembly location, handling both local dev and CI layouts.
- **Config factory** — a `TestConfigBuilder` fluent builder that produces `DependencyGeneratorConfig` instances with sensible defaults overridable per test (avoids repeated boilerplate).
- **`SolutionProjectBuilder`** — an in-memory builder for `SolutionProject` objects used in unit tests to avoid constructing complex nested object graphs by hand.
- **Path scrubber** — a Verify `ScrubInlineGuids` / custom scrubber that replaces fixture absolute paths with a stable placeholder.
- **Snapshot approval workflow** — document in repo `README` (or separate `CONTRIBUTING.md`) how to run tests in approval mode locally (`VERIFY_AUTO_APPROVE=1`) vs. CI strict mode.

---

## 7. Out of Scope

- Image export (png, svg, pdf) — requires external CLI tools; not suitable for automated tests without environment setup.
- `DependencyGenerator.CreateDiagramsAsync()` end-to-end as a single black-box test — too broad; the pipeline is already covered by the combination of parser, renderer, and folder structure scenarios above.
- Performance / load tests.
- Testing the `DiagramGeneratorSample` sample application itself.

---

## 8. Decisions and Constraints

1. **Fixture package choices**: Use real public NuGet packages in fixture solutions.

2. **Committed assets files**: Commit `obj/project.assets.json` for fixture projects.

3. **Framework reference for CI portability**: Use a non-Windows-specific framework reference for the `FrameworkRefs` fixture so scenarios can run on future non-Windows CI.

4. **Solution organization**: Keep all test projects and fixture-related test projects in the main solution.

5. **Grouping coverage**: Include dedicated unit tests for multi-version group node detection in addition to integration coverage.

6. **Parser boundary**: Keep `SolutionParser` as integration-tested only; no additional unit-only extraction is planned.
