# Sln Dependency Studio CLI

The Studio CLI is a command-line tool that reads a SlnDependencyStudio configuration (`.sds`) file, and generates dependency diagrams for a Visual Studio solution — showing which projects depend on which, with full package and framework reference graphs.

You can also configure it to run a command automatically before generation starts (for example, `dotnet restore`), so your solution assets are always up to date.

## Quick Start

### Prerequisites

- The generator relies on assets written by Visual Studio to determine the dependencies. Your solution must be restored or built so each project has the required `obj/project.assets.json` files. See the section on pre-generation commands where you could automatically run `dotnet restore` prior to generating the diagrams.
- For image export (PNG, SVG, PDF):
  - [D2 CLI](https://d2lang.com/tour/install/) — required when generating D2 images.
  - [Mermaid CLI (mmdc)](https://github.com/mermaid-js/mermaid-cli#installation) — required when generating Mermaid images.

### Build and Install

```powershell
# Publish the CLI to a folder of your choice, for example:
dotnet publish Studio\SlnDependencyStudio.Cli -o D:\tools\SlnDependencyStudio

# Add that folder to your PATH so the tool can be invoked from anywhere
# (adjust the path to match your chosen location)
$env:Path += ";D:\tools\SlnDependencyStudio"
```

After the tool is on your PATH, all examples in this document assume you can run it directly:

## Commands

The CLI has two commands. Both require the `--configFile` (or `--cf`) option pointing to a `.sds` configuration file.

### `validate`

Checks a `.sds` configuration file for errors — missing values, invalid paths, malformed settings — without running any generation.

```text
SlnDependencyStudio.Cli validate --cf <path-to-sds-file>
```

Use this first when setting up a new configuration or troubleshooting an existing one.

### `run`

Loads a `.sds` configuration file, validates it, optionally runs a pre-generation command, and then generates the dependency diagrams as per the configuration settings.

```text
SlnDependencyStudio.Cli run --cf <path-to-sds-file>
```

### Path Resolution

- The `--cf` path is resolved relative to your current working directory (or used as-is if absolute).
- All paths inside the `.sds` file (like `solutionPath`, `rootPath`, or `workingDirectory`) are resolved relative to the folder containing the `.sds` file. This lets you store configs anywhere and move them without rewriting paths.

## Configuration File (`.sds`)

A `.sds` file is a JSON document that tells the CLI what to do. The `.sds` extension is just a convention — the content is standard JSON.

### Structure Overview

```json
{
  "schemaVersion": 1,
  "metadata": {
    "projectName": "My Solution",
    "description": "Dependency diagrams for My Solution"
  },
  "diagramGenerator": {
    "projects": { ... },
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

### `metadata`

Identifies the configuration file. Not used during generation.

| Field         | Description                        |
| ------------- | ---------------------------------- |
| `projectName` | A friendly name for the project.   |
| `description` | A short description of the config. |

### `diagramGenerator`

This is the main configuration section. It has three sub-sections.

#### `projects` — What to analyse

| Field                 | Description                                                             |
| --------------------- | ----------------------------------------------------------------------- |
| `solutionPath`        | Path to your `.sln` or `.slnx` file (relative to the `.sds` file).      |
| `regexToInclude`      | One or more regex patterns matching the projects you want to process.   |
| `regexToExclude`      | Optional regex patterns to exclude certain projects.                    |
| `packagesToExclude`   | Optional NuGet package IDs to leave out of diagrams (case-insensitive). |
| `frameworksToExclude` | Optional framework reference IDs to leave out (case-insensitive).       |
| `individual`          | Settings for generating per-project diagrams.                           |
| `all`                 | Settings for generating a single combined solution diagram.             |

Both `individual` and `all` support:

| Field                 | Description                                                                                  |
| --------------------- | -------------------------------------------------------------------------------------------- |
| `enabled`             | Whether to generate this scope.                                                              |
| `includeDependencies` | Whether to include framework and package dependencies.                                       |
| `transitiveDepth`     | How many levels of transitive (indirect) packages to show. `0` means no transitive packages. |

#### `diagram` — How the diagram looks

| Field             | Description                                                                |
| ----------------- | -------------------------------------------------------------------------- |
| `direction`       | Flow direction: `LR` (left-to-right), `RL`, `TB` (top-to-bottom), or `BT`. |
| `frameworkStyle`  | Fill style for framework nodes (`fill` colour, `opacity`).                 |
| `packageStyle`    | Fill style for package dependency nodes.                                   |
| `transitiveStyle` | Fill style for transitive (indirect) package nodes.                        |
| `groupName`       | Title for the group of projects on the diagram.                            |
| `groupNameAlias`  | Short alias used in generated D2 files.                                    |
| `grouping`        | Whether to render grouping containers, and their background style.         |
| `formats`         | Which diagram formats to generate: `"D2"` and/or `"Mermaid"`.              |

#### `export` — Where and how to save results

| Field           | Description                                                                                                                               |
| --------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `clearContents` | Clears the output folder before writing new files.                                                                                        |
| `rootPath`      | Root directory for generated files (resolved relative to the `.sds` file). A sub-folder is created per target framework and per renderer. |
| `imageFormats`  | Image types to render: `"Png"`, `"Svg"`, and/or `"Pdf"`. Leave empty for text-only output (`.d2` / `.mmd` files plus a summary).          |

Image rendering requires the corresponding CLI tool on your PATH (see [Prerequisites](#prerequisites)).

### `preGeneration` — Optional command to run before generation

Sometimes you need a step to run before the generator can read your solution — like `dotnet restore` to create the asset files.

| Field               | Description                                                                                                                          |
| ------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `enabled`           | Set to `true` to activate the pre-generation command.                                                                                |
| `command`           | The executable to run (e.g. `dotnet`, `cmd.exe`).                                                                                    |
| `arguments`         | Arguments passed to the command. Tokens are split by spaces.                                                                         |
| `workingDirectory`  | Working directory for the command. Leave empty to use the `.sds` file's folder. Relative paths are resolved against the `.sds` file. |
| `continueOnFailure` | When `true`, generation proceeds even if this command exits with an error code.                                                      |

#### Example: restore before generation

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

### Complete `.sds` Example

```json
{
  "schemaVersion": 1,
  "metadata": {
    "projectName": "SlnDependencyDiagramGenerator",
    "description": "Generates dependency diagrams for the SlnDependencyDiagramGenerator solution"
  },
  "diagramGenerator": {
    "projects": {
      "solutionPath": "..\\..\\SlnDependencyDiagramGenerator.sln",
      "regexToInclude": ["\\.*\\.csproj"],
      "regexToExclude": ["\\(Tests|Studio)\\"],
      "packagesToExclude": ["Microsoft.Build", "Microsoft.SourceLink.GitHub"],
      "frameworksToExclude": ["Microsoft.NETCore.App"],
      "individual": {
        "enabled": true,
        "includeDependencies": true,
        "transitiveDepth": 2
      },
      "all": {
        "enabled": true,
        "includeDependencies": true,
        "transitiveDepth": 1
      }
    },
    "diagram": {
      "direction": "LR",
      "frameworkStyle": { "fill": "#ECCBC0", "opacity": 0.8 },
      "packageStyle": { "fill": "#ADD8E6", "opacity": 0.8 },
      "transitiveStyle": { "fill": "#FFEC96", "opacity": 0.8 },
      "groupName": "Dependency Diagram Generator",
      "groupNameAlias": "ddg",
      "grouping": {
        "enabled": false,
        "backgroundStyle": { "fill": "#E7EBFC", "opacity": 1.0 }
      },
      "formats": ["D2", "Mermaid"]
    },
    "export": {
      "clearContents": true,
      "rootPath": "..\\..\\Output",
      "imageFormats": ["Png", "Svg", "Pdf"]
    }
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

## Sample Configuration Files

The CLI folder includes ready-to-use `.sds` files:

| File            | Description                                                                                                                                                                                  |
| --------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `sample.sds`    | Configured for the solution in this repository. D2 + Mermaid, PNG + SVG + PDF export. Pre-generation disabled.                                                                               |
| `alloverit.sds` | Configured for the AllOverIt solution ([external repo](https://github.com/mjfreelancing/AllOverIt)). D2 only, PNG + SVG export. Pre-generation enabled (`dotnet restore` before generation). |

## Exit Codes

When scripting the CLI, check the exit code to determine the outcome:

| Code   | Meaning                                                           |
| ------ | ----------------------------------------------------------------- |
| `0`    | Success.                                                          |
| `1001` | Command-line arguments could not be parsed.                       |
| `1002` | The `--cf` path does not exist.                                   |
| `1003` | The `validate` command found configuration errors.                |
| `1004` | The `run` command failed (pre-generation or generator error).     |
| `1005` | Pre-generation command failed and `continueOnFailure` is `false`. |
| `1006` | The diagram generator threw an error.                             |
| `1999` | An unexpected failure occurred.                                   |

> When exit code `1005` is returned, the specific reason is indicated by one of the following sub-error codes (present in logs only):
>
> | Sub-code | Reason                                          |
> | -------- | ----------------------------------------------- |
> | `1`      | Pre-generation command was cancelled.           |
> | `2`      | Pre-generation command timed out.               |
> | `999`    | Unexpected error during pre-generation command. |
