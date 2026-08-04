# SlnDependencyStudio CLI User Guide

SlnDependencyStudio CLI is a cross-platform command-line tool that reads a dependency project (`.sds`) file and generates dependency diagrams for a Visual Studio solution. It is designed for automation in scripts and CI pipelines.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Commands](#commands)
  - [Validate Command](#validate-command)
  - [Run Command](#run-command)
- [Configuration File (.sds)](#configuration-file-sds)
- [Exit Codes](#exit-codes)
- [Logging and Output](#logging-and-output)
- [Path Resolution](#path-resolution)
- [Pre-Generation Commands](#pre-generation-commands)
- [Examples](#examples)
- [Sample Files](#sample-files)

---

## Prerequisites

1. **.NET 10.0 SDK** (or the version matching your build) to publish or run the CLI.
2. **Solution restore** — The target solution must be restored or built so each project has `obj/project.assets.json` available. See [Pre-Generation Commands](#pre-generation-commands) for automating this.
3. **External tools for image export** (optional):
   - [D2 CLI](https://github.com/terrastruct/d2/blob/master/docs/INSTALL.md) — required when generating D2 images (PNG/SVG/PDF).
   - [Mermaid CLI (mmdc)](https://github.com/mermaid-js/mermaid-cli) — required when generating Mermaid images.

---

## Installation

### Build and Publish

```powershell
# Publish the CLI to a folder of your choice
dotnet publish Studio\SlnDependencyStudio.Cli -o D:\tools\SlnDependencyStudio

# Add that folder to your PATH
$env:Path += ";D:\tools\SlnDependencyStudio"
```

After adding to PATH, you can invoke the tool as `SlnDependencyStudio.Cli` from anywhere.

### Verify Installation

```shell
SlnDependencyStudio.Cli --help
```

---

## Commands

The CLI has two commands: `validate` and `run`. Both require the `--configFile` (or `--cf`) option pointing to a `.sds` configuration file.

### Validate Command

Checks a `.sds` configuration file for errors — missing values, invalid paths, malformed settings — without running any generation.

```
SlnDependencyStudio.Cli validate --cf <path-to-sds-file>
```

**Typical use:** Run this first when setting up a new configuration or troubleshooting an existing one. Validation includes:

- JSON parse correctness
- Pre-generation command configuration validity
- Diagram generator configuration validity (solution path, regex patterns, export settings, etc.)

### Run Command

Loads a `.sds` configuration file, validates it, optionally runs a pre-generation command, and then generates dependency diagrams as per the configuration settings.

```
SlnDependencyStudio.Cli run --cf <path-to-sds-file>
```

**Pipeline:**

1. Load and deserialize the `.sds` file
2. Resolve all relative paths to absolute paths
3. Log the resolved configuration (for troubleshooting)
4. Validate pre-generation command settings
5. Validate the main diagram generator configuration
6. Execute the pre-generation command (if enabled)
7. Run diagram generation (project discovery → dependency resolution → diagram emission → optional image export)
8. Report completion or errors

---

## Exit Codes

The CLI returns deterministic exit codes suitable for script automation.

| Exit Code | Enum Constant                | Meaning                                                                        |
| --------- | ---------------------------- | ------------------------------------------------------------------------------ |
| `0`       | —                            | Success                                                                        |
| `1001`    | `CommandLineParseFailed`     | Command-line argument parsing failed                                           |
| `1002`    | `CannotLoadConfigFile`       | Config file not found, inaccessible, or malformed JSON                         |
| `1003`    | `ValidateCommandFailed`      | The `validate` command found configuration errors                              |
| `1004`    | `RunCommandFailed`           | The `run` command failed (validation errors, regex errors, cancellation, etc.) |
| `1005`    | `PreGenerationCommandFailed` | Pre-generation command failed and `continueOnFailure` is disabled              |
| `1006`    | `DiagramGeneratorFailed`     | The diagram generator threw an error during `CreateDiagramsAsync`              |
| `1007`    | `DiagramToolNotFound`        | A required external diagram tool (d2, mmdc) was not found                      |
| `1999`    | `UnhandledCliFailure`        | An unexpected failure occurred                                                 |

---

## Logging and Output

### Console Output

- Uses **Serilog** with `Serilog.Sinks.Console` and `AnsiConsoleTheme.Code` for colorized output.
- Log levels are color-coded: Error (red), Warning (yellow), Information (white), Debug (gray).
- Standard output and error output use separate logging channels.

### Rolling File Logs

- Logs are written to a `logs` subfolder relative to the `.sds` file being processed.
- File naming pattern: `{configFileBaseName}-{Date}.txt` (e.g. `sample-2026-07-25.txt`).
- This provides a persistent record for troubleshooting past runs.
- Rolling file logs always capture **all log levels (Debug and above)**; the `--verbose` flag does not change what is written to the file.

### Verbosity

The generator and renderers emit informational messages about project discovery, framework processing, and diagram creation, all of which appear in both the console and rolling file logs.

The console shows **Information level and above** by default; passing `--verbose` / `-v` lowers the console level to **Debug**. Rolling file logs always capture Debug level and above regardless of the `--verbose` flag.

---

## Path Resolution

- The `--configFile` / `--cf` path is resolved relative to your current working directory (or used as-is if absolute).
- All paths inside the `.sds` file — `solutionPath`, `rootPath`, `workingDirectory` — are resolved relative to the folder containing the `.sds` file. This allows you to store configs anywhere and move them without rewriting paths.

---

## Pre-Generation Commands

The pre-generation command runs before diagram generation. It is configured in the `.sds` file under the `preGeneration` section.

**Example: restore before generation**

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

**When `continueOnFailure` is `true`**, the command's stdout and stderr are still streamed to the log, but generation proceeds even on non-zero exit codes. This is useful when the pre-generation step is non-critical (e.g. a cleanup script that may already be clean).

**When `continueOnFailure` is `false`** (the default), generation is aborted immediately and exit code `1005` is returned.

Output from the pre-generation command (stdout/stderr) is streamed in real time to the console and rolling log.

---

## Examples

### Basic Validation

```shell
SlnDependencyStudio.Cli validate --cf .\Studio\SlnDependencyStudio.Cli\sample.sds
```

### Run Generation

```shell
SlnDependencyStudio.Cli run --cf .\Studio\SlnDependencyStudio.Cli\sample.sds
```

### Run with Restore First

Using the `.sds` file's pre-generation section configured for `dotnet restore`:

```shell
SlnDependencyStudio.Cli run --cf my-project.sds
```

### Script Automation

```powershell
# PowerShell
$result = & SlnDependencyStudio.Cli run --cf project.sds
if ($LASTEXITCODE -ne 0) {
    Write-Error "Generation failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}
```

---

## Sample Files

The repository includes sample `.sds` files in `Studio\SlnDependencyStudio.Cli\`:

- **`sample.sds`** — Configuration for the `SlnDependencyDiagramGenerator` solution, excluding test/studio projects and overriding packages/frameworks.
- **`alloverit.sds`** — Configuration for the `AllOverIt` solution, with pre-generation restore enabled, and filtering out test/demo/benchmark projects.

These serve as reference configurations for creating your own `.sds` files.
