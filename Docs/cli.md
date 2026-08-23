# SlnDependencyStudio CLI User Guide

SlnDependencyStudio CLI is a cross-platform command-line tool that reads a dependency project (`.sds`) file and generates dependency diagrams for a Visual Studio solution — showing which projects depend on which, with full package and framework reference graphs.

It is built for automation: it can validate configurations, restore the solution, run optional pre/post-generation commands, and returns deterministic exit codes that scripts and CI pipelines can rely on.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Getting Help](#getting-help)
- [Commands](#commands)
  - [Shared Options](#shared-options)
  - [Validate Command](#validate-command)
  - [Run Command](#run-command)
- [The Project File (.sds)](#the-project-file-sds)
- [Path Resolution](#path-resolution)
- [The Generation Pipeline](#the-generation-pipeline)
  - [Solution Restore](#solution-restore)
  - [Pre-Generation Command](#pre-generation-command)
  - [Diagram Generation](#diagram-generation)
  - [Post-Generation Command](#post-generation-command)
- [Exit Codes](#exit-codes)
- [Logging and Output](#logging-and-output)
- [External Tool Requirements](#external-tool-requirements)
- [Examples](#examples)
- [Scripting and CI](#scripting-and-ci)
- [Troubleshooting](#troubleshooting)
- [Sample Files](#sample-files)

---

## Prerequisites

1. **.NET SDK** — The CLI targets `net10.0` (a matching .NET SDK is required to publish or run it).
2. **Solution restore** — The target solution must be restored or built so each project has `obj/project.assets.json`. The CLI can do this for you automatically (see [Solution Restore](#solution-restore)) or via a pre-generation command.
3. **External tools for image export** (optional: PNG/SVG/PDF):
   - [D2 CLI](https://d2lang.com/tour/install/) — required when rendering D2 images.
   - [Mermaid CLI (mmdc)](https://github.com/mermaid-js/mermaid-cli#installation) — required when rendering Mermaid images.

---

## Installation

### Download a Pre-Built Binary

Pre-built versions of the CLI are distributed as part of the **SlnDependencyStudio** application on the [Releases page](https://github.com/mjfreelancing/SlnDependencyDiagramGenerator/releases). The release is an **installer** that installs SlnDependencyStudio — both the WPF application and the CLI — to a location of your choice; no .NET SDK is required. After installing, add the folder containing the CLI executable to your PATH so you can invoke it as `SlnDependencyStudio.Cli` from anywhere (the CLI is installed in a `CLI` sub-folder under your chosen install location).

Alternatively, build it yourself:

### Build and Publish

```powershell
# Publish the CLI to a folder of your choice
dotnet publish Studio\SlnDependencyStudio.Cli -o D:\tools\SlnDependencyStudio

# Add that folder to your PATH (adjust the path to match your chosen location)
$env:Path += ";D:\tools\SlnDependencyStudio"
```

After adding the folder to your PATH, you can invoke the tool as `SlnDependencyStudio.Cli` from anywhere.

### Verify Installation

```shell
SlnDependencyStudio.Cli --help
```

---

## Getting Help

Every command supports `--help` (or `-h`):

```shell
# Top-level help — lists commands and shared options
SlnDependencyStudio.Cli --help

# Per-command help — options for a specific command
SlnDependencyStudio.Cli run --help
SlnDependencyStudio.Cli validate --help
```

---

## Commands

The CLI has two commands: `validate` and `run`. Both require the `--projectFile` (or `--pf`) option pointing to a `.sds` project file.

### Shared Options

| Option          | Alias  | Required | Description                                                                                                                                                           |
| --------------- | ------ | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `--projectFile` | `--pf` | Yes      | Path to the `.sds` project file. Resolved relative to the current working directory (or used as-is if absolute).                                                      |
| `--verbose`     | `-v`   | No       | Enable Debug-level logging on the console. Does not affect the rolling file log, which always captures Debug and above (see [Rolling File Logs](#rolling-file-logs)). |
| `--help`        | `-h`   | No       | Show help and usage information.                                                                                                                                      |

> The `--verbose` flag works before or after the subcommand (e.g. `SlnDependencyStudio.Cli --verbose run --pf x.sds` or `SlnDependencyStudio.Cli run --verbose --pf x.sds`).

### Validate Command

Checks a `.sds` project file for errors — missing values, invalid paths, malformed settings, invalid regex patterns — **without running any generation or pipeline commands**.

```text
SlnDependencyStudio.Cli validate --pf <path-to-sds-file>
```

**What it checks:**

- JSON parse correctness
- Document structure (`schemaVersion`, `metadata`, `diagramGenerator`, `preGeneration`, `postGeneration`, `restoreSolution`)
- Solution configuration (path exists, at least one scope enabled, transitive depth, filters)
- Diagram configuration (at least one format, valid colours, grouping)
- Export configuration (root path set)
- Pre/post-generation command configuration (command non-empty when enabled, working directory exists)

**Exit codes:**

| Exit Code | Meaning                                                                          |
| --------- | -------------------------------------------------------------------------------- |
| `0`       | Valid — the configuration passed validation.                                     |
| `1001`    | Command-line argument parsing failed.                                            |
| `1002`    | The project file could not be loaded (missing, inaccessible, or malformed JSON). |
| `1003`    | Validation failed.                                                               |
| `1013`    | Cancelled by the user (**Ctrl+C** / SIGTERM).                                    |
| `1014`    | Operation cancelled internally.                                                  |
| `1999`    | An unexpected failure occurred.                                                  |

See [Exit Codes](#exit-codes) for the full reference (including the additional codes the `run` command can return).

**Typical use:** Run this first when setting up a new configuration or troubleshooting an existing one, before attempting a full `run`.

### Run Command

Loads a `.sds` project file, validates it, then executes the full generation pipeline:

```text
SlnDependencyStudio.Cli run --pf <path-to-sds-file>
```

**Pipeline:**

1. Load and deserialise the `.sds` file and resolve relative paths
2. Log the resolved configuration (for troubleshooting)
3. Validate the whole project up front — failures are reported before any command or generation work begins
4. Run the pre-generation command if enabled (aborts on failure unless `continueOnFailure`)
5. Restore the solution if `restoreSolution` is enabled
6. Generate diagrams (project discovery → dependency resolution → framework processing → diagram emission → optional image export)
7. Run the post-generation command if enabled
8. Report completion or errors

---

## The Project File (.sds)

A `.sds` file is a JSON document (the extension is just a convention). It describes what to analyse, how diagrams should look, where to write them, and which pipeline steps to run.

The complete field-level reference is in [configuration.md](./configuration.md). A fully populated example of the document shape:

```json
{
  "schemaVersion": 1,
  "metadata": {
    "projectName": "My Solution",
    "description": "Dependency diagrams for My Solution"
  },
  "diagramGenerator": {
    "solution": {
      "solutionPath": "MySolution.sln",
      "regexToInclude": ["\\.*\\.csproj"],
      "regexToExclude": ["\\.*Tests\\.csproj"],
      "packagesToExclude": ["Some.Package"],
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
      "groupName": "My Solution",
      "groupNameAlias": "my",
      "grouping": {
        "enabled": true,
        "backgroundStyle": {
          "fill": "#E7EBFC",
          "opacity": 1
        }
      },
      "formats": ["D2", "Mermaid"]
    },
    "export": {
      "clearContents": true,
      "rootPath": "output",
      "imageFormats": ["Png", "Svg", "Pdf"]
    }
  },
  "restoreSolution": true,
  "preGeneration": {
    "enabled": false,
    "command": "",
    "arguments": "",
    "workingDirectory": "",
    "continueOnFailure": false
  },
  "postGeneration": {
    "enabled": false,
    "command": "",
    "arguments": "",
    "workingDirectory": ""
  }
}
```

Key points:

- `diagramGenerator` holds the generator configuration (solution, diagram, export) and is shared with the WPF application.
- `restoreSolution` (default `true`) runs `dotnet restore` on the solution before generation.
- `preGeneration` / `postGeneration` run optional commands around the generation step.
- Unknown fields are preserved on save for forward compatibility.

---

## Path Resolution

- The `--projectFile` / `--pf` path is resolved relative to your current working directory (or used as-is if absolute).
- Paths inside the `.sds` file — `solutionPath`, `rootPath`, `workingDirectory` — are resolved relative to the folder containing the `.sds` file **when stored as relative paths**. This keeps the file portable: you can move the whole project tree (the `.sds` plus its solution and output) to a new location or machine without rewriting paths. Absolute paths point to fixed disk locations instead, so they keep working if only the `.sds` file moves, but they do not follow the solution/output if the project is relocated.

---

## The Generation Pipeline

### Pre-Generation Command

Runs before the solution restore and diagram generation — the earliest user hook in the pipeline. Configured under `preGeneration`:

```json
{
  "preGeneration": {
    "enabled": true,
    "command": "dotnet",
    "arguments": "nuget add source C:\\packages\\my-feed --name MyFeed",
    "workingDirectory": "",
    "continueOnFailure": false
  }
}
```

| Field               | Description                                                                                                                             |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| `enabled`           | When `true`, the command runs before generation.                                                                                        |
| `command`           | The executable or script to run (e.g. `dotnet`, `cmd.exe`, `powershell.exe`).                                                           |
| `arguments`         | Command-line arguments, split by spaces.                                                                                                |
| `workingDirectory`  | Working directory for the command. Empty defaults to the `.sds` file's folder.                                                          |
| `continueOnFailure` | When `true`, generation proceeds even if the command exits with an error; when `false` (default), the run aborts with exit code `1005`. |

Output from the command is streamed in real time to the console and rolling log.

> **Security note:** Pre-generation commands execute whatever they are told to. Only run `.sds` files you trust.

> **Ordering note:** the pre-generation command runs **before** the solution is restored, so it cannot rely on `obj/project.assets.json` being present. Use it for setup that must happen before the built-in restore (e.g. generating project files, preparing a custom feed) — not for steps that consume restored packages.

### Solution Restore

When `restoreSolution` is `true` (the default), the CLI runs `dotnet restore` against the solution after any pre-generation command and before diagram generation. This guarantees each project has an up-to-date `obj/project.assets.json`.

- Restore output (stdout/stderr) is streamed to the console and rolling log in real time.
- If the restore fails, the run aborts with exit code `1009`.

Set `"restoreSolution": false` to skip this step (for example, when you manage restore yourself or want a fully offline run).

### Diagram Generation

The core step — project discovery, dependency resolution, framework processing, diagram emission, and optional image export — as configured in `diagramGenerator`. All output appears in both the console and the rolling file log.

### Post-Generation Command

Runs after diagram generation completes. Configured under `postGeneration`:

```json
{
  "postGeneration": {
    "enabled": true,
    "command": "powershell.exe",
    "arguments": "-File .\\notify.ps1",
    "workingDirectory": ""
  }
}
```

`postGeneration` shares the same `command`, `arguments`, and `workingDirectory` fields as `preGeneration` (it has no `continueOnFailure` option — a failed post-generation command aborts the run with exit code `1006`).

---

## Exit Codes

The CLI returns deterministic exit codes suitable for script automation.

| Exit Code | Enum Constant                 | Meaning                                                                     |
| --------- | ----------------------------- | --------------------------------------------------------------------------- |
| `0`       | —                             | Success                                                                     |
| `1001`    | `CommandLineParseFailed`      | Command-line argument parsing failed                                        |
| `1002`    | `CannotLoadProjectFile`       | Project file not found, inaccessible, or malformed JSON                     |
| `1003`    | `ValidateCommandFailed`       | The `validate` command found configuration errors                           |
| `1004`    | `RunCommandFailed`            | The `run` command failed (validation errors, regex errors, etc.)            |
| `1005`    | `PreGenerationCommandFailed`  | Pre-generation command failed and `continueOnFailure` is disabled           |
| `1006`    | `PostGenerationCommandFailed` | Post-generation command failed                                              |
| `1007`    | `DiagramGeneratorFailed`      | The diagram generator threw an error during `CreateDiagramsAsync`           |
| `1008`    | `DiagramToolNotFound`         | A required external diagram tool (d2, mmdc) was not found                   |
| `1009`    | `DotNetRestoreFailed`         | `dotnet restore` failed while `restoreSolution` was enabled                 |
| `1010`    | `DiagramImageExportFailed`    | Diagram image export failed (via d2 or mmdc)                                |
| `1011`    | `ProjectAssetsFailed`         | Project assets could not be read (missing assets file / unsupported format) |
| `1012`    | `DependencyGraphFailed`       | Project dependency graph is inconsistent (missing reference / circular)     |
| `1013`    | `UserCancelled`               | The command was cancelled by the user (Ctrl+C/SIGTERM)                      |
| `1014`    | `OperationCancelled`          | An operation was cancelled internally (not a user shutdown request)         |
| `1999`    | `UnhandledCliFailure`         | An unexpected failure occurred                                              |

When a pre-generation or restore command fails, the log also reports the failure classification (`ErrorCode`):

| ErrorCode                  | Meaning                                                      |
| -------------------------- | ------------------------------------------------------------ |
| `Cancelled`                | The command was cancelled before or during execution         |
| `UnexpectedError`          | The command could not be started (e.g. executable not found) |
| `ProcessExitedWithFailure` | The command ran and exited with a non-zero exit code         |

---

## Logging and Output

### Console Output

- Uses **Serilog** with an `AnsiConsoleTheme.Code`-based console sink for colourised output.
- Log levels are colour-coded: Error (red), Warning (yellow), Information (white), Debug (grey).
- Standard output and error output use separate logging channels.

### Rolling File Logs

Every run also writes to a rolling file log, independent of the console's `--verbose` filter.

- **Location:** A `logs` subfolder **next to the `.sds` file being processed** (e.g. `<sds-folder>\logs\`). If no project file is available, the log falls back to a `logs` subfolder under the CLI's own directory.
- **Naming:** One file per day, using the pattern `{projectFileBaseName}-yyyyMMdd.txt` — for example, a project named `sample.sds` produces `sample-20260814.txt`. A fallback log (no project file) uses `studio-yyyyMMdd.txt`.
- **Retention:** Log files are kept for **31 days** by default (Serilog's default retention; this is not configurable in the CLI).
- **Verbosity:** Rolling file logs always capture **all log levels (Debug and above)** — the `--verbose` flag changes only the console output, not what is written to the file.
- **Purpose:** This provides a persistent record for troubleshooting past runs.

### Verbosity

- The console shows **Information level and above** by default.
- Passing `--verbose` / `-v` lowers the console level to **Debug**, showing generator/renderer detail alongside the informational output.

---

## External Tool Requirements

| Tool                     | Required for                       | Notes                                                                                                              |
| ------------------------ | ---------------------------------- | ------------------------------------------------------------------------------------------------------------------ |
| **D2 CLI (`d2`)**        | D2 image export (PNG/SVG/PDF)      | Only needed when `formats` includes `D2` **and** `imageFormats` is non-empty. `.d2` text files need no tool.       |
| **Mermaid CLI (`mmdc`)** | Mermaid image export (PNG/SVG/PDF) | Only needed when `formats` includes `Mermaid` **and** `imageFormats` is non-empty. `.mmd` text files need no tool. |

- Tools are located via explicit path overrides first, then PATH discovery (using `where` on Windows, `which` elsewhere).
- If a required tool is missing during a run, the CLI reports it and returns exit code `1008`.
- Diagram text files (`.d2`/`.mmd`) and the `Dependency Summary.md` are always produced even when image export fails or is disabled.

---

## Examples

### 1. Show help

```shell
SlnDependencyStudio.Cli --help
SlnDependencyStudio.Cli run --help
```

### 2. Validate a project file

```shell
SlnDependencyStudio.Cli validate --pf .\Studio\SlnDependencyStudio.Cli\sample.sds
```

### 3. Run generation

```shell
SlnDependencyStudio.Cli run --pf .\Studio\SlnDependencyStudio.Cli\sample.sds
```

### 4. Run with verbose logging

```shell
SlnDependencyStudio.Cli run --pf my-project.sds --verbose
```

### 5. Run with automatic restore

With `"restoreSolution": true` in the `.sds` file (the default), the solution is restored as part of the pipeline, before diagram generation:

```shell
SlnDependencyStudio.Cli run --pf my-project.sds
```

### 6. Run with a pre-generation command

Restore is handled by the built-in `restoreSolution` step, so a pre-generation command is best reserved for setup the restore depends on. For example, adding a NuGet package source before restore runs:

```json
{
  "preGeneration": {
    "enabled": true,
    "command": "dotnet",
    "arguments": "nuget add source C:\\packages\\my-feed --name MyFeed",
    "workingDirectory": "",
    "continueOnFailure": false
  }
}
```

```shell
SlnDependencyStudio.Cli run --pf my-project.sds
```

### 7. Run with a post-generation command

A `.sds` configured to open the output folder after generation (Windows):

```json
{
  "postGeneration": {
    "enabled": true,
    "command": "explorer.exe",
    "arguments": "C:\\Output\\MySolution",
    "workingDirectory": ""
  }
}
```

### 8. Portable paths

Store the `.sds` anywhere, as long as the paths inside it are stored as **relative** paths — they resolve relative to the `.sds` file's own folder, so you can move the whole project tree (`.sds`, solution, and output) to a new location or machine and it keeps working. Absolute paths point to fixed disk locations, so they keep working when only the `.sds` moves, but they will be left pointing at the old paths if you relocate the project.

```text
C:\repos\my-solution\
├── SlnDependencyDiagramGenerator\
│   └── project.sds      # solutionPath: "..\my-solution.sln", rootPath: "..\Output"
├── my-solution.sln
└── Output\
```

```shell
SlnDependencyStudio.Cli run --pf C:\repos\my-solution\SlnDependencyDiagramGenerator\project.sds
```

---

## Sample File

The repository includes a ready-to-use sample `.sds` file:

`Studio\SlnDependencyStudio.Cli\sample.sds` — Configuration for the `SlnDependencyDiagramGenerator` solution:

- Restore enabled
- Test/Studio projects excluded.
- D2 + Mermaid output
- PNG + SVG + PDF export

Use it as a reference when creating your own `.sds` files.
