# Configuration Reference

This document covers every configuration option available in `SlnDependencyDiagramGenerator`. Configuration is defined via `DependencyGeneratorConfig` (in code) or serialized as JSON in `.sds` files (used by SlnDependencyStudio CLI and WPF).

---

## Quick Links

- [Diagram Generator Configuration](#diagram-generator-configuration)
  - [Solution Options](#solution-options)
  - [Diagram Options](#diagram-options)
  - [Export Options](#export-options)
- [Studio Project Document (.sds) Format](#studio-project-document-sds-format)
  - [Metadata](#metadata)
  - [Pre-Generation Command](#pre-generation-command)
- [Target Framework Discovery](#target-framework-discovery)
- [Tool Requirements](#tool-requirements)
- [Architecture Overview](#architecture-overview)

---

## Diagram Generator Configuration

The `DependencyGeneratorConfig` class is the root configuration object. It has three sub-sections:

| Property   | Type                       | Description                                                     |
| ---------- | -------------------------- | --------------------------------------------------------------- |
| `Solution` | `GeneratorSolutionOptions` | Solution path, filters, exclusions, and per-scope configuration |
| `Diagram`  | `GeneratorDiagramOptions`  | Diagram styling, direction, grouping, and output formats        |
| `Export`   | `GeneratorExportOptions`   | Export path and image format options                            |

### Solution Options

Defined by `GeneratorSolutionOptions`.

| Property              | Type           | Default | Description                                                                                                                                                                                                  |
| --------------------- | -------------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `SolutionPath`        | `string`       | `""`    | Relative or fully-qualified path to the `.sln` or `.slnx` solution file.                                                                                                                                     |
| `RegexToInclude`      | `string[]`     | `[]`    | One or more regex patterns to match solution projects to be processed. To match all `.csproj` files under a path, use a pattern like `\\.*\\.csproj`. Note that backslashes must be escaped in JSON strings. |
| `RegexToExclude`      | `string[]`     | `[]`    | Optional regex patterns to exclude matched projects.                                                                                                                                                         |
| `PackagesToExclude`   | `string[]`     | `[]`    | Optional NuGet package IDs to exclude from diagram and summary output. Case-insensitive. Transitive dependencies reachable only through excluded packages are also omitted.                                  |
| `FrameworksToExclude` | `string[]`     | `[]`    | Optional framework reference IDs to exclude from diagram and summary output. Case-insensitive.                                                                                                               |
| `Individual`          | `ProjectScope` | —       | Settings for generating per-project diagrams (see [Project Scope](#project-scope)).                                                                                                                          |
| `All`                 | `ProjectScope` | —       | Settings for generating a single combined solution diagram (see [Project Scope](#project-scope)).                                                                                                            |

#### Project Scope

Defined by `GeneratorSolutionOptions.ProjectScope`. Shared by both `Individual` and `All`.

| Property              | Type   | Default | Description                                                                                                                      |
| --------------------- | ------ | ------- | -------------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`             | `bool` | `false` | Whether this scope is processed. At least one scope must be enabled.                                                             |
| `IncludeDependencies` | `bool` | `false` | Whether framework and package dependencies are included in the diagram.                                                          |
| `TransitiveDepth`     | `int`  | `0`     | How many levels of transitive (indirect) package references to traverse. `0` means no transitive packages. Must be 0 or greater. |

**Scope behavior:**

- **Individual scope** (`Individual`): When enabled, the generator produces one diagram per matching project. Each diagram shows that project and its direct dependencies (framework references, explicit packages, and transitive packages up to the configured depth).
- **All scope** (`All`): When enabled, the generator produces a single combined diagram showing all matching projects and their collective dependency graph. This is useful for understanding the full solution-level dependency picture.

Both scopes can be enabled simultaneously. The generator uses the maximum transitive depth across enabled scopes when resolving the package graph.

### Diagram Options

Defined by `GeneratorDiagramOptions`.

| Property          | Type               | Default | Description                                                                                                                                                                                                            |
| ----------------- | ------------------ | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Direction`       | `DiagramDirection` | `LR`    | Flow direction of the diagram. Options: `LR` (left-to-right), `RL` (right-to-left), `TB` (top-to-bottom), `BT` (bottom-to-top).                                                                                        |
| `FrameworkStyle`  | `FillStyle`        | —       | Fill style for framework dependency nodes (see [Fill Style](#fill-style)).                                                                                                                                             |
| `PackageStyle`    | `FillStyle`        | —       | Fill style for explicit package dependency nodes (see [Fill Style](#fill-style)).                                                                                                                                      |
| `TransitiveStyle` | `FillStyle`        | —       | Fill style for transitive (implicit) package dependency nodes (see [Fill Style](#fill-style)).                                                                                                                         |
| `GroupName`       | `string`           | `""`    | The display name (title) for the group of projects on the diagram.                                                                                                                                                     |
| `GroupNameAlias`  | `string`           | `""`    | A short alias used in generated diagram files to represent the project group. This is a technical identifier (not visible in rendered image output) that enables visual grouping of projects in D2 and Mermaid syntax. |
| `Grouping`        | `GroupingOptions`  | —       | Grouping behavior and style for diagram containers (see [Grouping Options](#grouping-options)).                                                                                                                        |
| `Formats`         | `DiagramFormat[]`  | `[]`    | Diagram formats to generate. Must contain at least one format. Options: `D2`, `Mermaid`.                                                                                                                               |

#### Fill Style

Defined by `GeneratorDiagramOptions.FillStyle`. Used by `FrameworkStyle`, `PackageStyle`, `TransitiveStyle`, and `GroupingOptions.BackgroundStyle`.

| Property  | Type     | Description                                                           |
| --------- | -------- | --------------------------------------------------------------------- |
| `Fill`    | `string` | CSS or RGB fill color (e.g. `"#ECCBC0"`, `"#ADD8E6"`).                |
| `Opacity` | `double` | Opacity value between 0.0 (fully transparent) and 1.0 (fully opaque). |

#### Grouping Options

Defined by `GeneratorDiagramOptions.GroupingOptions`.

| Property          | Type        | Default                       | Description                                                                                                                                     |
| ----------------- | ----------- | ----------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| `Enabled`         | `bool`      | `true`                        | When `true`, project and multi-version package grouping containers are rendered. When `false`, all nodes are rendered without group containers. |
| `BackgroundStyle` | `FillStyle` | Fill `#E7EBFC`, Opacity `1.0` | The fill style used for grouping container backgrounds.                                                                                         |

**Grouping behavior:**

When grouping is enabled, the generator creates visual containers that group:

1. **Projects** — All projects in the current scope are rendered inside a group container labelled with `GroupName`.
2. **Multi-version packages** — When the same package appears with different resolved versions across projects, a grouping container is created for that package to highlight the version conflict.

### Export Options

Defined by `GeneratorExportOptions`.

| Property        | Type                   | Default | Description                                                                                                                                                                                                                                  |
| --------------- | ---------------------- | ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ClearContents` | `bool`                 | `false` | When `true`, clears the target framework output folder and diagram-format sub-folders (e.g. `d2/`, `mmd/`) before writing new files. Only sub-folders for configured formats are cleared.                                                    |
| `RootPath`      | `string`               | `""`    | Relative or fully-qualified export root path for generated diagram files and images. The generator creates sub-folders per target framework, and within each, per diagram renderer (e.g. `<RootPath>/net9.0/d2/`, `<RootPath>/net9.0/mmd/`). |
| `ImageFormats`  | `DiagramImageFormat[]` | `[]`    | Image formats to create from diagram files. Can be empty (text-only output) or any combination of: `Png`, `Svg`, `Pdf`.                                                                                                                      |

**Output folder structure:**

```
<RootPath>/
├── <target-framework-1>/
│   ├── d2/
│   │   ├── <project-name>-Individual.d2
│   │   ├── <project-name>-Individual.png
│   │   ├── <solution-scope>-All.d2
│   │   └── <solution-scope>-All.png
│   └── mmd/
│       ├── <project-name>-Individual.mmd
│       ├── <project-name>-Individual.png
│       ├── <solution-scope>-All.mmd
│       └── <solution-scope>-All.png
└── <target-framework-2>/
    └── ...
```

**File naming:**

Generated diagram and image files use normalized file-safe base names. For example, a project named `My.Project` with the Individual scope becomes `my-project-Individual.d2`. The solution-scope All diagram becomes `my-solution-all.d2`.

---

## Studio Project Document (.sds) Format

The `.sds` file is the saved dependency project document format used by SlnDependencyStudio CLI and WPF. It wraps the `DependencyGeneratorConfig` in an extensible envelope.

```json
{
  "schemaVersion": 1,
  "metadata": {
    "projectName": "My Solution",
    "description": "Dependency diagrams for My Solution"
  },
  "diagramGenerator": {
    "solution": { ... },
    "diagram": { ... },
    "export": { ... }
  },
  "preGeneration": {
    "enabled": false,
    "command": "",
    "arguments": "",
    "workingDirectory": "",
    "continueOnFailure": false
  }
}
```

| Property           | Type     | Description                                                                                                        |
| ------------------ | -------- | ------------------------------------------------------------------------------------------------------------------ |
| `schemaVersion`    | `int`    | Schema version for forward compatibility. Currently `1`.                                                           |
| `metadata`         | `object` | User-facing project metadata (see [Metadata](#metadata)).                                                          |
| `diagramGenerator` | `object` | The `DependencyGeneratorConfig` payload (see [Diagram Generator Configuration](#diagram-generator-configuration)). |
| `preGeneration`    | `object` | Optional pre-generation command configuration (see [Pre-Generation Command](#pre-generation-command)).             |

**Forward compatibility:** Unknown JSON fields are preserved via `[JsonExtensionData]`. Editing a document with a newer schema version does not strip data from unknown fields.

### Metadata

Defined by `DependencyProjectMetadata`.

| Field         | Type     | Description                                                                            |
| ------------- | -------- | -------------------------------------------------------------------------------------- |
| `projectName` | `string` | A friendly display name for the dependency project. Not used during generation.        |
| `description` | `string` | An optional description of the project's purpose or scope. Not used during generation. |

### Pre-Generation Command

Defined by `PreGenerationConfig`. Controls an optional command that executes before diagram generation starts.

| Field               | Type     | Default | Description                                                                                                                                                        |
| ------------------- | -------- | ------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `enabled`           | `bool`   | `false` | When `true`, the pre-generation command runs before diagram generation.                                                                                            |
| `command`           | `string` | `""`    | The executable or script to run (e.g. `"dotnet"`, `"cmd.exe"`, `"powershell.exe"`).                                                                                |
| `arguments`         | `string` | `""`    | Command-line arguments passed to the command. Tokens are split by spaces.                                                                                          |
| `workingDirectory`  | `string` | `""`    | Working directory for the command. When empty, defaults to the folder containing the `.sds` file. Relative paths are resolved against the `.sds` file's directory. |
| `continueOnFailure` | `bool`   | `false` | When `true`, diagram generation proceeds even if the pre-generation command exits with a non-zero exit code. When `false`, generation is aborted on failure.       |

**Common use cases:**

Run `dotnet restore` before generation to ensure `project.assets.json` files are up to date:

```json
{
  "preGeneration": {
    "enabled": true,
    "command": "dotnet",
    "arguments": "restore MySolution.sln",
    "workingDirectory": "",
    "continueOnFailure": false
  }
}
```

**Path resolution (CLI):** In the CLI, relative paths inside the `.sds` file (`solutionPath`, `rootPath`, `workingDirectory`) are resolved relative to the folder containing the `.sds` file. In the WPF application, the user can choose whether to store paths as absolute or relative to the `.sds` file directory.

---

## Target Framework Discovery

Target frameworks are **auto-discovered** from each matching project's `obj/project.assets.json` file. You do not need to specify `targetFrameworks` in configuration.

**Requirements:**

- The solution must be restored or built **before** generation so each project has its `obj/project.assets.json` file. If these files are missing or stale, run `dotnet restore` or build the solution first.

**How it works:**

1. The generator reads each matching project's `obj/project.assets.json` to discover its target frameworks.
2. All unique target frameworks across all matching projects are collected.
3. The solution is parsed once per target framework, resolving project and framework references from evaluated MSBuild items, and package graphs from assets data.

**Note:** Malformed or unreadable solution files (`.sln` or `.slnx`) fail fast with a clear error message; no fallback parsing is attempted.

---

## Tool Requirements

### D2 Diagrams

- **Diagram generation** (`.d2` files): No external tools required.
- **Image export** (PNG, SVG, PDF): Requires the [D2 CLI](https://github.com/terrastruct/d2/blob/master/docs/INSTALL.md) to be available on PATH (or configured via an explicit path override).

**PNG export notes:** D2's PNG export may have specific requirements depending on your platform. See the [D2 export documentation](https://d2lang.com/ko/tour/exports/) for details on potential issues and workarounds.

### Mermaid Diagrams

- **Diagram generation** (`.mmd` files): No external tools required.
- **Image export** (PNG, SVG, PDF): Requires [Mermaid CLI (mmdc)](https://github.com/mermaid-js/mermaid-cli) to be available on PATH (or configured via an explicit path override).

### Tool Detection

Tool detection uses a layered resolution strategy:

1. **Explicit path override** — If configured (in WPF application settings or via code), the override path is used directly.
2. **PATH discovery** — Uses platform-appropriate commands (`where` on Windows, `which` on non-Windows) via `AllOverIt.Process` to locate the tool on the system PATH.

---

## Architecture Overview

The generator uses a staged pipeline:

```
Configuration → Validation → Framework Discovery → Solution Parse →
Dependency Resolution → Graph Model → Intermediate Representation → Renderer Emission → Optional Image Export
```

1. **Configuration load and bind** — The host application loads configuration and binds it to `DependencyGeneratorConfig`.
2. **Validation** — Configuration is validated using FluentValidation rules, throwing on violations.
3. **Target framework discovery** — Target frameworks are discovered from each project's `project.assets.json`.
4. **Solution parsing** — For each target framework, solution projects are parsed, MSBuild items are evaluated, and package graphs are resolved from assets data.
5. **Graph model construction** — A `DependencyGraphModel` is built for each enabled scope (individual/all), including multi-version package grouping metadata.
6. **Intermediate representation** — Each renderer produces a renderer-neutral intermediate representation (nodes, edges, styles, groups).
7. **Renderer emission** — `D2DiagramRenderer` and `MermaidDiagramRenderer` serialize the IR into `.d2` and `.mmd` files.
8. **Optional image export** — If image formats are configured, the generated diagram files are rendered via the D2 CLI and/or Mermaid CLI into PNG, SVG, and/or PDF.

The generator, all renderers, and all supporting services use `ILogger<T>` (from `Microsoft.Extensions.Logging`) as the logging abstraction, with `NullLogger<T>.Instance` as the no-op fallback when no logger is registered.
