# Samples

This folder contains a runnable demonstration of the generator and a deliberate conflict project used to prove summary conflict detection.

## Folder Layout

- `DiagramGeneratorSample/` — Console app that loads config and runs `DependencyGenerator`.
- `NugetConflictSample/` — Minimal project with intentionally different package versions.
- `Directory.Build.props` — Adds a `ProjectReference` from `DiagramGeneratorSample` to `Source/SlnDependencyDiagramGenerator.csproj`.
- `Output/` — Generated artifacts grouped by target framework (`net8.0`, `net9.0`, `net10.0`).

## What Each Project Does

### DiagramGeneratorSample

- Target framework: `net10.0`.
- Entrypoint: `DiagramGeneratorSample/Program.cs`.
- Configuration source: `appsettings.json` plus optional variant override via `SETTINGS_VARIANT`.
- Optional explicit config path: `--configFile <path>` command argument.

The app is intentionally thin: it binds `options` to `DependencyGeneratorConfig`, constructs `DependencyGenerator`, and executes `CreateDiagramsAsync()`.

### NugetConflictSample

- Target framework: `net10.0`.
- Purpose: create cross-project package-version conflicts in the generated summary.
- Deliberate package versions:
  - `AllOverIt` `9.2.0`
  - `NuGet.Protocol` `7.3.1`

These differ from the main generator project (`Source/SlnDependencyDiagramGenerator.csproj`), which references newer versions (`AllOverIt` `9.2.1`, `NuGet.Protocol` `7.6.0`).

This is intentional so `Dependency Summary.md` can show conflict rows that are easy to validate.

## Why Conflict Results Appear Only in net10.0

`NugetConflictSample` only targets `net10.0`. The library project targets `net8.0`, `net9.0`, and `net10.0`.

Because conflict analysis is performed within the discovered graph per target framework:

- `net10.0` includes both projects -> conflict section appears.
- `net8.0` and `net9.0` do not include `NugetConflictSample` -> no cross-project conflict table.

This behavior is expected and useful for validating framework-specific graph differences.

## Configuration Model in the Sample

The sample uses a base config plus optional variant overlays.

### Base config

File: `DiagramGeneratorSample/appsettings.json`

Key defaults:

- `solution.solutionPath`: points to `SlnDependencyDiagramGenerator.sln`.
- `solution.regexToInclude`: includes `.csproj` files.
- `solution.packagesToExclude`: excludes `Microsoft.Build` and `Microsoft.SourceLink.GitHub` from diagrams/summary.
- `solution.frameworksToExclude`: excludes `Microsoft.NETCore.App`.
- `solution.individual.transitiveDepth`: `2`.
- `solution.all.transitiveDepth`: `1`.
- `diagram.formats`: empty array.
- `export.rootPath`: `..\\..\\..\\..\\Output` (resolves to `Samples/Output`).
- `export.imageFormats`: `png`, `svg`, `pdf`.

Why `diagram.formats` is empty in base:

- Variant files override this array.
- Keeping base empty avoids accidental mixed-mode output when experimenting with variants.

### Variant overrides

Set `SETTINGS_VARIANT` to load `appsettings.{variant}.json` in addition to base.

| Variant value | File                    | Effective behavior              |
| ------------- | ----------------------- | ------------------------------- |
| `d2`          | `appsettings.d2.json`   | D2 only, grouping enabled       |
| `mmd`         | `appsettings.mmd.json`  | Mermaid only, grouping disabled |
| `both`        | `appsettings.both.json` | D2 + Mermaid                    |

`launchSettings.json` defaults to `SETTINGS_VARIANT=both` for local debugging.

## How to Run

From repository root:

```powershell
dotnet run --project Samples/DiagramGeneratorSample
```

Run with explicit variant:

```powershell
$env:SETTINGS_VARIANT = "d2"
dotnet run --project Samples/DiagramGeneratorSample
```

Run with custom standalone config:

```powershell
dotnet run --project Samples/DiagramGeneratorSample -- --configFile Samples/DiagramGeneratorSample/appsettings.custom.json
```

Notes:

- Use a copied/custom file (for example `appsettings.custom.json`) when exercising `--configFile` so behavior is isolated from the shared base config.
- If `--configFile` is used, the specified file is loaded as primary configuration.
- If `SETTINGS_VARIANT` is also set, variant overlay is still applied.
- For fully predictable custom runs, unset `SETTINGS_VARIANT` when using `--configFile`.

## Expected Output Structure

Generated files are written to `Samples/Output/<tfm>/`.

Within each TFM folder:

- `Dependency Summary.md`
- `d2/` when D2 generation is enabled
- `mmd/` when Mermaid generation is enabled

File names for all-scope diagrams are normalized to lowercase, file-safe slugs.
Example: `Dependency Diagram Generator-All` -> `dependency-diagram-generator-all.*`.

## Tooling Prerequisites for Image Export

When `export.imageFormats` is not empty:

- D2 rendering requires `d2` on PATH.
  - Install/docs: [d2 CLI installation](https://d2lang.com/tour/install/)
- Mermaid image rendering requires `mmdc` on PATH.
  - Install/docs: [Mermaid CLI (mmdc)](https://github.com/mermaid-js/mermaid-cli#installation)

If either CLI is missing, image export will fail for the relevant renderer.

Text artifacts (`Dependency Summary.md`, `.d2`, `.mmd`) can still be used to inspect dependencies even without image tooling.

## Common Tweaks While Exploring

- Increase `projects.individual.transitiveDepth` or `projects.all.transitiveDepth` to expose deeper package graphs.
- Set `projects.frameworksToExclude` to `[]` to include framework references like `Microsoft.NETCore.App`.
- Set `projects.packagesToExclude` to `[]` to include tooling/build packages in output.
- Toggle `diagram.grouping.enabled` to compare grouped vs flat diagrams.
- Change `diagram.direction` (`LR`, `RL`, `TB`, `BT`) to optimize readability for your graph size.

## Troubleshooting

- Missing dependencies or empty graph:
  - Run `dotnet restore` so each project has `obj/project.assets.json`.
- No diagrams generated:
  - Ensure a non-empty `diagram.formats` value is active (base + variant).
- No conflict table:
  - Confirm target framework includes both projects with conflicting packages (for this sample, use `net10.0`).
- Output path confusion:
  - `rootPath` in sample config resolves relative to the executing sample project directory.

## Related Docs

- Root usage and architecture: `README.md`
- Product requirements: `PRDs/v4/Implementation PRD.md`
- Testing plan: `PRDs/v4/Testing PRD.md`
