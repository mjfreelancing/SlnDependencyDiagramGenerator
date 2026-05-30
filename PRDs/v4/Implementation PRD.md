# Product Requirements Document — SlnDependencyDiagramGenerator v4

**Date:** May 2026  
**Status:** Draft  
**Scope:** Full rewrite with parity + enhancements

---

## 1. Executive Summary

`SlnDependencyDiagramGenerator` parses a Visual Studio solution, resolves NuGet dependency graphs, and produces D2 diagram files plus rendered images and a Markdown summary. The library is functional and well-structured, but its dependency-resolution strategy has several correctness gaps — most critically that it does not follow NuGet's official conflict-resolution rules, does not support `Directory.Packages.props` (Central Package Management), and only partially supports `Directory.Build.props`. Additionally, the authoritative, fully-resolved package graph is already available on disk in every SDK-style project after `dotnet restore` via `project.assets.json`, and is not being used.

A targeted rewrite is recommended. It retains all current public API surface and configuration contracts while replacing the internal resolution engine entirely with `NuGet.ProjectModel`'s lock-file reader — the same source NuGet and MSBuild use themselves. The remote-feed resolver is removed; `dotnet restore` is a declared prerequisite.

---

## 2. Current State Analysis

### 2.1 How the Current System Works

| Component                    | Location                                         | Responsibility                                                                                                                                                                                         |
| ---------------------------- | ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `SolutionParser`             | `Source/Parser/SolutionParser.cs`                | Reads solution files, iterates projects, and extracts `ProjectReference`, `PackageReference`, `FrameworkReference`, and target-framework properties from project XML (without full MSBuild evaluation) |
| `NugetPackageResolver`       | `Source/Parser/NugetPackageResolver.cs`          | For each explicit `PackageReference`, fetches dependency info from a configured NuGet feed using `DependencyInfoResource.ResolvePackage()`, then recurses to a configured depth                        |
| `DependencyGenerator`        | `Source/Generator/DependencyGenerator.cs`        | Orchestrates parsing, then generates D2 content and invokes the `d2` CLI for image output                                                                                                              |
| `SummaryDependencyGenerator` | `Source/Generator/SummaryDependencyGenerator.cs` | Produces the `Dependency Summary.md` Markdown report                                                                                                                                                   |
| Configuration                | `Source/Config/`                                 | Strongly-typed POCO configuration loaded from `appsettings.json`                                                                                                                                       |

### 2.2 Known Deficiencies

#### 2.2.1 Version Conflict Resolution Is Incorrect

`NugetPackageResolver.GetPackageReferencesForDependencies()` always uses `dependency.VersionRange.MinVersion.ToFullString()` as the resolved version of each transitive package. This means:

- When the same package appears via multiple dependency paths with different ranges, the tool picks the minimum version declared by _one arbitrary path_, not the version NuGet would actually select at build time.
- NuGet uses a **"nearest wins"** algorithm: the dependency closest to the project root wins; among ties, the highest satisfying version wins. None of this logic is implemented.
- Results shown in the diagram and summary report can therefore differ from what `dotnet build` actually uses, making the output misleading.

#### 2.2.2 `Directory.Build.props` Not Supported

`GetTargetFrameworks()` reads only the `TargetFramework` / `TargetFrameworks` property directly from the project's `ItemGroup`/`PropertyGroup` XML. If a project inherits its `TargetFramework` from a `Directory.Build.props` file higher in the directory tree, the tool throws `DependencyGeneratorException` with the message _"does not specify a target framework. Importing of Directory.Build.Props is not supported."_

This affects any repo that uses a centralised `TargetFramework` or shared property group.

#### 2.2.3 `Directory.Packages.props` (Central Package Management) Not Supported

When Central Package Management (CPM) is enabled, `PackageReference` items in project files deliberately omit the `Version` attribute — the version is defined centrally in `Directory.Packages.props`. The current `GetNormalisedPackageVersion()` call will receive `null` or an empty string and either throw or produce an invalid version string, making CPM solutions completely unusable with the tool.

#### 2.2.4 Network Round-Trips for Every Explicit Package

The current resolver contacts a NuGet feed for every explicit `PackageReference` (and recursively for each dependency). For large solutions this is slow and fragile in air-gapped or proxy-restricted environments. The fully-resolved information already exists locally in `obj/project.assets.json` after `dotnet restore` — a step that must already have been performed before any build.

#### 2.2.5 No Mermaid Output Format

Only D2 and rendered D2 images (PNG/SVG/PDF) are produced. Mermaid is a widely supported diagram format (GitHub, VS Code, Confluence, Notion) with no external binary dependency, but it is not offered.

---

## 3. Goals and Non-Goals

### Goals

1. Preserve the full existing public API surface where practical (`DependencyGenerator`, `DependencyGeneratorConfig` and child config types), with explicitly documented v4 breaking configuration changes.
2. Replace the resolution engine with `NuGet.ProjectModel` lock-file reading so that resolved package versions exactly match what `dotnet build` uses.
3. Declare `dotnet restore` as a hard prerequisite; fail fast with a clear diagnostic if `project.assets.json` is absent or stale for any selected project.
4. Support `Directory.Build.props` for properties including `TargetFramework`, `TargetFrameworks`, and `Version` (handled automatically once the assets file is the source).
5. Support `Directory.Packages.props` (Central Package Management) — also handled automatically via the assets file.
6. Emit accurate version-conflict information in diagrams and the summary report, showing the winning version and which path(s) requested a different version.
7. Add Mermaid as an output format, selectable alongside or instead of D2.
8. Auto-discover and process all target frameworks present in each project's `project.assets.json` (the `targets` keys). The `targetFrameworks` setting is removed from the root `options` object; the frameworks to process are no longer the caller's concern.
9. Improve external tool execution reliability: validate required tools using buffered process execution, support Windows `mmdc.cmd` invocation semantics, and avoid noisy console output during validation.
10. Prevent output collisions when both D2 and Mermaid image exports are enabled by using renderer-specific output file names.
11. Document the rewrite plan and all breaking changes in this PRD and in inline code comments.

### Non-Goals

- A GUI or VS Code extension.
- Parsing legacy `packages.config` projects.
- Support for `.vbproj` or `.fsproj` files (these continue to work if they are SDK-style, but are not explicitly tested).
- Real-time watch/incremental mode.
- Remote NuGet feed queries of any kind (the feed resolver is removed).
- Private-feed credential configuration (no longer needed; feeds are only consulted by `dotnet restore`, which is outside the tool's scope).

---

## 4. Functional Requirements

### FR-1: Solution and Project Parsing

| ID     | Requirement                                                                                                                                                                                                                                                                                         |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-1.1 | Parse both `.sln` and `.slnx` solution files to enumerate projects. Use the appropriate solution reader for each format and normalize the project list into one shared downstream model.                                                                                                            |
| FR-1.2 | For each project, evaluate the fully-resolved MSBuild model (not raw XML) so that properties inherited from `Directory.Build.props`, `Directory.Build.targets`, and other import chains are visible. Use `Microsoft.Build.Evaluation.ProjectCollection` for this.                                   |
| FR-1.3 | Enumerate the target frameworks to process from the `targets` keys in each project's `project.assets.json`. These are the frameworks that were actually restored and are the authoritative source. MSBuild-evaluated `TargetFramework`/`TargetFrameworks` properties are not used for this purpose. |
| FR-1.4 | Detect whether Central Package Management is active (`ManagePackageVersionsCentrally == true`) either from the evaluated model or by locating `Directory.Packages.props` in the directory hierarchy.                                                                                                |
| FR-1.5 | If a solution file is malformed or unreadable (`.sln` or `.slnx`), fail fast with a clear error message containing the path and reason. No automatic fallback to another format/parser and no partial processing.                                                                                   |
| FR-1.6 | Add parity tests for malformed solution-file error handling across both supported formats (`.sln` and `.slnx`).                                                                                                                                                                                     |

### FR-2: Package Version Resolution

| ID     | Requirement                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-2.1 | **Sole strategy — `project.assets.json` reader.** Parse `obj/project.assets.json` using `NuGet.ProjectModel.LockFileUtilities.GetLockFile()`. Read the `targets` section for the relevant target framework to obtain the authoritative, fully-resolved package closure. This source embodies NuGet's "nearest wins" conflict resolution, CPM version pins, and `Directory.Build.props` property evaluation — all handled by the prior `dotnet restore` run. |
| FR-2.2 | If `project.assets.json` is absent or its format version is less than 3, abort with a clear error message instructing the user to run `dotnet restore` first. No fallback to remote feeds.                                                                                                                                                                                                                                                                  |
| FR-2.3 | Clearly distinguish **explicit** references (present in `project.frameworks[tf].dependencies` in the assets file) from **transitive** references (present in `targets[tf]` but not in the explicit set).                                                                                                                                                                                                                                                    |
| FR-2.4 | When a package version differs from what one or more transitive paths requested, record the conflict for display in the diagram and summary.                                                                                                                                                                                                                                                                                                                |
| FR-2.5 | `Directory.Build.props` and `Directory.Packages.props` require no special handling — their effects are already baked into `project.assets.json` by the restore. Document this clearly in code comments.                                                                                                                                                                                                                                                     |

### FR-3: Diagram Output — D2 (existing behaviour retained)

| ID     | Requirement                                                                                                                                                                              |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-3.1 | Retain all D2 generation logic including direction, fill style, group name/alias, and multi-version package grouping.                                                                    |
| FR-3.2 | Retain support for rendering D2 files to PNG, SVG, and PDF via the `d2` CLI.                                                                                                             |
| FR-3.3 | When a package has multiple versions in the solution (i.e., different projects resolved different winning versions), annotate the diagram node to make this visible (current behaviour). |

### FR-4: Diagram Output — Mermaid (new)

| ID     | Requirement                                                                                                                                                                                                                                                                                                                                                 |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-4.1 | Add Mermaid diagram generation to `GeneratorDiagramOptions` using a `formats` collection (array) with values: `D2` and/or `Mermaid` (`D2` default). The legacy single `format` field and `Both` enum value are removed.                                                                                                                                     |
| FR-4.2 | When `formats` includes `Mermaid`, generate a `.mmd` file per project scope (individual / all) using Mermaid `flowchart` syntax, mirroring the same relationships and styling as the D2 output.                                                                                                                                                             |
| FR-4.3 | Apply node styling to framework, explicit package, and transitive package nodes using Mermaid's `style` directive, mapping from the existing `FrameworkStyle`, `PackageStyle`, and `TransitiveStyle` fill/opacity configuration.                                                                                                                            |
| FR-4.4 | Mermaid `.mmd` file generation must not require any external binary or network call.                                                                                                                                                                                                                                                                        |
| FR-4.5 | The existing `imageFormats` config option is shared across both D2 and Mermaid output. Both renderers support `png`, `svg`, and `pdf` (D2 via the `d2` CLI; Mermaid via [mermaid-cli](https://github.com/mermaid-js/mermaid-cli), `mmdc`). When Mermaid image output is requested, `mmdc` must be available on `PATH` — it is a prerequisite, not optional. |
| FR-4.6 | Mermaid labels with multiple lines must render correctly using `<br>` line breaks (not escaped newlines), ensuring version text appears on separate lines in Mermaid-compatible renderers.                                                                                                                                                                  |
| FR-4.7 | Mermaid subgraphs that contain all emitted nodes must explicitly declare `direction` inside the `subgraph` block to preserve configured orientation (`LR` / `RL` / `TB` / `BT`).                                                                                                                                                                            |
| FR-4.8 | When exporting images for both D2 and Mermaid, generated files must be placed into renderer-specific subfolders under the target framework output folder (`d2/` and `mmd/`) using a normalized file-safe base diagram name (lowercase slug), to make the preferred format easier to find and avoid collisions.                                              |
| FR-4.9 | On Windows, Mermaid image export must invoke `mmdc` via `cmd.exe /c` to correctly resolve `.cmd` executables on `PATH` (`PATHEXT` behavior).                                                                                                                                                                                                                |

### FR-5: Dependency Summary Report

| ID     | Requirement                                                                                                                                                                                                                                                |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-5.1 | Retain the existing `Dependency Summary.md` format.                                                                                                                                                                                                        |
| FR-5.2 | When version conflicts are detected (same package, different winning versions across projects), add a **Version Conflicts** section to the summary listing the package, each project's resolved version, and the paths that requested a different version. |
| FR-5.3 | Target framework badges must be generated dynamically from discovered frameworks, including `.NET 10.0` and future target frameworks without requiring code changes.                                                                                       |

### FR-6: Configuration Schema

| ID     | Requirement                                                                                                                                                                                                                                                                                                                                                                                      |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| FR-6.1 | `packageFeeds` and `targetFrameworks` are both removed from `DependencyGeneratorConfig` and `appsettings.json` in v4. These are deliberate breaking changes: feed resolution is gone, and target frameworks are now auto-discovered from each project's assets file. Existing config files will need to remove both fields from the root `options` object.                                       |
| FR-6.2 | Replace `diagram.format` (single value) with `diagram.formats` (array of values: `"d2"`, `"mermaid"`). At least one value must be defined; empty arrays are invalid.                                                                                                                                                                                                                             |
| FR-6.3 | Replace direction values `"left"`, `"right"`, `"up"`, `"down"` with standard flow notation `"LR"`, `"RL"`, `"TB"`, `"BT"`.                                                                                                                                                                                                                                                                       |
| FR-6.4 | Add a new optional `packagesToExclude` string array under `projects`. Each entry is an exact package ID (case-insensitive). Any matching package is omitted from all diagrams and the summary report. Transitive dependencies reachable only through excluded packages are also omitted; those reachable via another non-excluded path are retained. Defaults to an empty array (no exclusions). |
| FR-6.5 | Add a new optional `frameworksToExclude` string array under `projects`. Each entry is an exact framework reference ID (case-insensitive). Any matching framework reference is omitted from all diagrams and the summary report. Defaults to an empty array (no exclusions).                                                                                                                      |

---

## 5. Non-Functional Requirements

| ID    | Requirement                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-1 | Target frameworks: `net8.0`, `net9.0`, `net10.0` (matching the current library).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| NFR-2 | No new mandatory runtime dependencies beyond those already used. `NuGet.ProjectModel` (already a transitive dependency of `NuGet.Protocol`) must be added as an explicit dependency.                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| NFR-3 | All public API types remain in the same namespaces.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| NFR-4 | Unit tests are required for the entire engine and are deferred to the final stage of the rewrite. The test project will be created by the author, who will specify the test framework and assertion libraries at that point. **When implementation of the engine is complete, prompt the author to create the test project before writing any tests.** Coverage must include: assets-file parsing, explicit vs. transitive classification, CPM project output, `Directory.Build.props` output, stale/missing file error path, and cross-project conflict detection.                                                                                        |
| NFR-5 | The tool must produce identical output to v3 for projects that have no `Directory.Build.props`, no CPM, and have `project.assets.json` available. **Success criteria:** (1) Run v4 against the existing Sample project and confirm its output matches the v3-generated output already committed to the repository (verified by no Git diff). (2) The author will create a second sample project that uses both `Directory.Build.props` and `Directory.Packages.props` (CPM); run v4 against it and commit its output as the baseline. Subsequent runs must produce no Git diff, confirming deterministic output for CPM/`Directory.Build.props` scenarios. |
| NFR-6 | Processing a 20-project solution should complete in under 30 seconds when using the `project.assets.json` primary strategy (no network I/O).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |

---

## 6. Technical Approach

### 6.1 Recommended Strategy: `project.assets.json` as the Authoritative Source

After `dotnet restore`, every SDK-style project writes a `project.assets.json` file to its `obj/` directory. This file is produced by NuGet's own restore engine and represents the definitive, fully-resolved package graph for each target framework. It:

- Applies NuGet's "nearest wins" conflict-resolution algorithm.
- Incorporates `Directory.Build.props` (already evaluated by MSBuild before restore).
- Incorporates `Directory.Packages.props` (Central Package Management).
- Distinguishes direct (`type: "direct"`) from transitive (`type: "transitive"`) dependencies (NuGet 6+ with `<RestorePackagesWithLockFile>` or via the `targets` → `dependencies` section).
- Is already on disk — no network calls.

The `NuGet.ProjectModel` package (part of the NuGet client SDK, same vendor as `NuGet.Protocol` already in use) provides `LockFileUtilities.GetLockFile(path, logger)` to deserialise this file.

#### Reading the Asset Graph

```csharp
using NuGet.ProjectModel;

var lockFile = LockFileUtilities.GetLockFile(
    Path.Combine(projectObjDir, "project.assets.json"),
    nugetLogger);

// For a specific target framework:
var target = lockFile.GetTarget(NuGetFramework.ParseFolder("net9.0"), runtimeIdentifier: null);

foreach (var library in target.Libraries)
{
    // library.Name   — package ID
    // library.Version — resolved (winning) version
    // library.Type   — "package" | "project"
    // library.Dependencies — direct children in the resolved graph
}
```

The `target.Libraries` collection is flat and deduplicated — each package ID appears exactly once at its winning version. Relationships between packages are available via `LockFileTargetLibrary.Dependencies`.

To distinguish direct (explicit) from transitive references, cross-reference `lockFile.PackageSpec.GetTargetFramework(framework).Dependencies` (the explicit set) against the full `target.Libraries` list.

### 6.2 Conflict Reporting

When multiple projects in a solution resolve the same package to _different_ winning versions, the tool should flag this. This is a cross-project comparison, distinct from NuGet's own within-project conflict resolution. The multi-project comparison logic in `DependencyGenerator.GetDeepOrderedDistinctPackageDependencies()` is retained, and each project's winning version is sourced from the assets file as the authoritative input.

### 6.3 `Directory.Build.props` and MSBuild Evaluation

MSBuild evaluation is used to read evaluated `ProjectReference` and `FrameworkReference` items for the active target framework (including import-chain contributions from `Directory.Build.props` / `Directory.Build.targets`). Target frameworks to process are discovered from `project.assets.json`, not from MSBuild properties. Replace raw `ProjectRootElement.Open()` with a full MSBuild evaluation via `ProjectCollection`:

```csharp
using var projectCollection = new ProjectCollection();
var project = projectCollection.LoadProject(
    projectPath,
    globalProperties: new Dictionary<string, string>
    {
        ["TargetFramework"] = targetFramework   // required for condition evaluation
    },
    toolsVersion: null);

var projectReferences = project.GetItems("ProjectReference");
var frameworkReferences = project.GetItems("FrameworkReference");
```

This automatically traverses `Directory.Build.props` imports through the standard MSBuild import chain without any custom file-search logic. `Directory.Packages.props` similarly requires no special handling here — its effect is already baked into the assets file.

> **Note on SDK resolution.** `Microsoft.Build.Evaluation.ProjectCollection` on .NET requires that `MSBuildLocator` (from `Microsoft.Build.Locator`) or an explicit SDK path be registered before loading projects. Resolver bootstrap must occur before parser construction, and registration must remain idempotent when re-checked during parsing.

### 6.4 Central Package Management (`Directory.Packages.props`)

No special handling is required. `project.assets.json` already contains the final resolved versions regardless of whether they were declared in individual project files, `Directory.Packages.props`, or via `GlobalPackageReference`. The assets file is the output of the full CPM resolution process.

### 6.5 Mermaid Generation

Mermaid `flowchart` format mirrors the D2 structure closely:

```
flowchart LR
    subgraph ddg["Dependency Diagram Generator"]
    direction LR
        MyProject
    end
  pkg1["PackageA<br>v1.2.3"]
    style pkg1 fill:#ADD8E6,opacity:0.8
    MyProject --> pkg1
  pkg2["TransitiveB<br>v2.0.0"]
    style pkg2 fill:#FFEC96,opacity:0.8
    pkg1 --> pkg2
```

Key mapping from D2 to Mermaid:

| D2 concept         | Mermaid equivalent                                            |
| ------------------ | ------------------------------------------------------------- |
| `direction: left`  | `flowchart LR`                                                |
| `direction: right` | `flowchart RL`                                                |
| `direction: up`    | `flowchart BT`                                                |
| `direction: down`  | `flowchart TB`                                                |
| `group { }`        | `subgraph`                                                    |
| `.style.fill`      | `style nodeId fill:#RRGGBB`                                   |
| `.style.opacity`   | `style nodeId opacity:0.8` (CSS, supported in most renderers) |
| `A <- B`           | `B --> A`                                                     |

Node IDs must be sanitised (letters, digits, hyphens only) identically to D2's alias generation. Node labels use `<br>` for Mermaid line breaks.

Tooling notes:

- Required external tools are validated using buffered process execution to avoid console noise and to ensure deterministic exit-code checks for both `d2` and `mmdc`.
- On Windows, Mermaid image generation invokes `mmdc` through `cmd.exe /c` so command resolution works for `mmdc.cmd` on `PATH`.

---

## 7. Breaking Changes (Complete List)

This section is normative for v4 migration.

### BC-1: `options.targetFrameworks` removed

- **Before (v3):** Caller supplied target frameworks.
- **After (v4):** Frameworks are auto-discovered from each project's `project.assets.json` targets.

Before:

```json
{
  "options": {
    "targetFrameworks": ["net8.0", "net9.0"]
    // other settings here
  }
}
```

After:

```json
{
  "options": {
    // other settings here
  }
}
```

### BC-2: `packageFeeds` removed

- **Before (v3):** Feed configuration used by internal resolver.
- **After (v4):** No feed configuration; restore is an external prerequisite.

Before:

```json
{
  "options": {
    "packageFeeds": [
      {
        "url": "https://api.nuget.org/v3/index.json"
      }
    ]
  }
}
```

After:

```json
{
  "options": {
    "projects": {
      "solutionPath": "..\\..\\..\\..\\SlnDependencyDiagramGenerator.sln",
      "regexToInclude": ["\\\\.*\\.csproj"],
      "regexToExclude": [],
      "packagesToExclude": ["Microsoft.Build", "Microsoft.SourceLink.GitHub"],
      "frameworksToExclude": ["Microsoft.NETCore.App"],
      "individual": {
        "enabled": true,
        "includeDependencies": true,
        "transitiveDepth": 1
      },
      "all": {
        "enabled": true,
        "includeDependencies": true,
        "transitiveDepth": 1
      }
    },
    "diagram": {
      "direction": "LR",
      "frameworkStyle": {
        "fill": "#ECCBC0",
        "opacity": 0.8
      },
      "packageStyle": {
        "fill": "#ADD8E6",
        "opacity": 0.8
      },
      "transitiveStyle": {
        "fill": "#FFEC96",
        "opacity": 0.8
      },
      "groupName": "Dependency Diagram Generator",
      "groupNameAlias": "ddg",
      "formats": ["d2", "mermaid"]
    },
    "export": {
      "clearContents": true,
      "rootPath": "..\\..\\..\\Output",
      "imageFormats": ["png", "svg", "pdf"]
    }
  }
}
```

### BC-3: `diagram.direction` values changed

- **Before (v3):** `"left"`, `"right"`, `"up"`, `"down"`
- **After (v4):** `"LR"`, `"RL"`, `"TB"`, `"BT"`

Before:

```json
{
  "options": {
    "diagram": {
      "direction": "left"
    }
  }
}
```

After:

```json
{
  "options": {
    "diagram": {
      "direction": "LR"
    }
  }
}
```

### BC-4: `diagram.format` replaced by `diagram.formats`

- **Before (v3):** Single value under `diagram.format`, including `"both"`.
- **After (v4):** Array under `diagram.formats` with one or more of `"d2"`, `"mermaid"`; `"both"` removed.

Before:

```json
{
  "options": {
    "diagram": {
      "format": "both"
    }
  }
}
```

After:

```json
{
  "options": {
    "diagram": {
      "formats": ["d2", "mermaid"]
    }
  }
}
```

### BC-5: `DiagramFormat.Both` removed in code

- **Before (v3):** `DiagramFormat` enum included `Both`.
- **After (v4):** `DiagramFormat` only includes `D2` and `Mermaid`; combine outputs by adding both entries to `Formats`.

Before:

```csharp
Diagram.Format = DiagramFormat.Both;
```

After:

```csharp
Diagram.Formats = new[] { DiagramFormat.D2, DiagramFormat.Mermaid };
```

The `MermaidDiagramRenderer` class should be a parallel to the existing D2 generation methods in `DependencyGenerator`, extracting shared graph-building logic into a common `DependencyGraphModel` that both renderers consume.

---

## 8. Architecture

### 8.1 Proposed Layer Structure

```
SlnDependencyDiagramGenerator/
├── Config/                          (unchanged — backward-compat config POCOs)
│   ├── DependencyGeneratorConfig.cs
│   ├── GeneratorDiagramOptions.cs   (+ DiagramFormat enum)
│   ├── GeneratorExportOptions.cs
│   ├── GeneratorProjectOptions.cs
│   └── NugetPackageFeed.cs          (DELETED)
│
├── Parser/
│   ├── SolutionParser.cs            (MSBuild evaluation instead of raw XML)
│   ├── ProjectAssetReader.cs        (NEW: reads project.assets.json via NuGet.ProjectModel)
│   └── ...existing models unchanged...
│
├── Generator/
│   ├── DependencyGenerator.cs       (orchestration, assets-file prerequisite check)
│   ├── DependencyGraphModel.cs      (NEW: shared intermediate representation for renderers)
│   ├── D2DiagramRenderer.cs         (extracted from DependencyGenerator — D2 rendering)
│   ├── MermaidDiagramRenderer.cs    (NEW: Mermaid rendering)
│   ├── SummaryDependencyGenerator.cs (version conflict section)
│   └── DiagramImageFormat.cs
│
├── Validators/                      (packageFeeds validator deleted)
└── Exceptions/                      (unchanged)
```

### 8.2 Assets File Prerequisite Check

Before any resolution begins, `DependencyGenerator` validates that every selected project has a usable `project.assets.json`:

```
For each selected project:
  IF obj/project.assets.json is missing:
    → Abort with: "Run 'dotnet restore' before generating diagrams. Missing: {path}"
  IF assets file version < 3:
    → Abort with: "project.assets.json format is too old. Re-run 'dotnet restore'."
```

No fallback to remote feeds. The requirement to have run `dotnet restore` is equivalent to the requirement to be able to build the solution — a reasonable and universally met precondition.

---

## 9. Rewrite Justification

A targeted rewrite (rather than incremental patching) is recommended for the following reasons:

1. **The resolution engine is the core of the library.** Patching `NugetPackageResolver` to add correct conflict resolution while simultaneously adding a completely different `ProjectAssetReader` path would result in two parallel code paths sharing almost no code, making the codebase harder to reason about.

2. **The intermediate representation is missing.** Currently, D2 content is generated inline inside `DependencyGenerator` methods. Adding Mermaid output requires extracting a shared graph model first. That extraction touches the same methods that need to change for the resolution improvements.

3. **The test surface is currently zero.** A rewrite is the right moment to add unit tests from the start, with clearly separated concerns that make unit testing practical.

4. **Scope is bounded.** The public API contracts (`DependencyGenerator`, all `Config` types), the configuration schema, and the D2/image output format are all _frozen_. The rewrite only replaces internal implementation.

---

## 10. Milestones

| Milestone                 | Scope                                                                                                                                                                                                                                                                           |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| M1 — Foundation           | `ProjectAssetReader` implementation + unit tests; updated `SolutionParser` using MSBuild evaluation; assets-file prerequisite check in `DependencyGenerator`                                                                                                                    |
| M2 — Rendering            | `DependencyGraphModel` intermediate representation; `D2DiagramRenderer` extracted; `MermaidDiagramRenderer` added; `DiagramFormat` config option                                                                                                                                |
| M3 — Summary + config     | Version-conflict section in `Dependency Summary.md`; dynamically generated target framework badges (including `.NET 10.0` and future frameworks); remove `NugetPackageFeed` and `targetFrameworks` config fields and associated validators; remaining config validation updates |
| M4 — Integration + sample | Sample updated to exercise CPM and `Directory.Build.props` (after restore); Mermaid output; full end-to-end test against the solution itself                                                                                                                                    |

---

## 11. Decisions

| #   | Question                                                                                                                                                                                                     | Decision                                                                                                                                                                                         |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| D-1 | Should Mermaid output be in `.mmd` (standard extension) or `.md` (for GitHub inline rendering)? Both could be offered.                                                                                       | Use `.mmd`.                                                                                                                                                                                      |
| D-2 | The `d2` CLI invocation uses `AllOverIt.Process`. Should this be replaced with a direct `System.Diagnostics.Process` call to reduce the `AllOverIt` dependency surface?                                      | Retain `AllOverIt.Process` — no change to the existing approach.                                                                                                                                 |
| D-3 | `MSBuildLocator` must be called before any MSBuild type is loaded. Should the library enforce this contract with a clear `DependencyGenerator.Initialize()` step, or document it as a caller responsibility? | Enforce it internally. `DependencyGenerator` bootstraps resolver initialization before parser construction and parsing performs an idempotent re-check. No public `Initialize()` API is exposed. |

---

## Appendix A — NuGet SDK Packages Required

| Package                   | Purpose                                                    | Already referenced?                              |
| ------------------------- | ---------------------------------------------------------- | ------------------------------------------------ |
| `NuGet.Protocol`          | `NuGetFramework` (framework parsing)                       | Yes — retained                                   |
| `NuGet.ProjectModel`      | `LockFileUtilities`, `PackageSpec`, `LockFileTarget`       | No — add explicitly                              |
| `NuGet.Versioning`        | `NuGetVersion` (version comparison for conflict reporting) | Yes (transitive) — make explicit                 |
| `Microsoft.Build`         | `ProjectCollection`, `Project`                             | Yes                                              |
| `Microsoft.Build.Locator` | SDK path registration                                      | No — add (already required by `Microsoft.Build`) |

`NuGet.Resolver` is **not** required. `NuGet.Protocol`'s feed-query classes (`DependencyInfoResource`, `SourceRepository`, `SourceCacheContext`) are no longer called at runtime and may be removed as a dependency if `NuGet.ProjectModel` does not transitively require them — check at implementation time.

---

## Appendix B — `project.assets.json` Key Fields Reference

```json
{
  "version": 3,
  "targets": {
    "net9.0": {
      "PackageId/1.2.3": {
        "type": "package",
        "dependencies": {
          "TransitiveDep": "2.0.0"
        }
      }
    }
  },
  "libraries": {
    "PackageId/1.2.3": {
      "type": "package",
      "path": "packageid/1.2.3"
    }
  },
  "project": {
    "frameworks": {
      "net9.0": {
        "dependencies": {
          "PackageId": {
            "target": "Package",
            "version": "[1.2.3, )"
          }
        }
      }
    }
  }
}
```

`targets[framework]` — flat resolved graph (one entry per package at its winning version).  
`project.frameworks[framework].dependencies` — explicit (direct) references only.  
Any package present in `targets` but absent from `project.frameworks.dependencies` is transitive.

---

## Appendix C — NuGet Conflict Resolution Rules (Reference)

NuGet's PackageReference restore uses the following priority order when the same package is required at different versions:

1. **Nearest wins:** The version requested by the dependency closest to the project root in the dependency graph wins. "Distance" is the number of edges from the project to the package.
2. **Explicit overrides highest-distance wins:** An explicit top-level `PackageReference` in the project file always wins over any transitive request, regardless of version.
3. **Tie-breaking:** When two paths are the same distance and neither is explicit, the higher-satisfying version is selected.
4. **CPM pin:** A `PackageVersion` in `Directory.Packages.props` acts as a top-level override for all projects in the tree, equivalent to an explicit `PackageReference` at that version.
5. **`VersionOverride`:** A `PackageReference` with `VersionOverride` takes precedence over the CPM-declared version for that project.

The `project.assets.json` file is the output of this algorithm and is the single source of truth.
