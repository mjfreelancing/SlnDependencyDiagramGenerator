# SlnDependencyStudio WPF User Guide

SlnDependencyStudio WPF is a Windows desktop application for authoring, editing, and running dependency diagram projects. It provides a graphical interface over the `SlnDependencyDiagramGenerator` library, letting you configure solutions, diagram options, export settings, and generation workflows without editing JSON files manually.

**Target platform:** Windows 10+ (`net10.0-windows10.0.19041`)

---

## Table of Contents

- [Quick Start](#quick-start)
- [Application Shell](#application-shell)
- [Navigation Sidebar](#navigation-sidebar)
- [Project Page](#project-page)
- [Solution Page](#solution-page)
- [Diagrams Page](#diagrams-page)
- [Export Page](#export-page)
- [Pipeline Page](#pipeline-page)
- [Generation Workflow](#generation-workflow)
- [Output Panel](#output-panel)
- [Application Settings](#application-settings)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [File Format](#file-format)

---

## Quick Start

1. Launch SlnDependencyStudio.
2. On the empty state screen, click **New Project** to create a dependency project from defaults, or **Open Project** to load an existing `.sds` file.
3. Navigate through the sidebar sections to configure your project:
   - **Project** — Set the project name and description.
   - **Solution** — Select your `.sln` or `.slnx` file and configure filters.
   - **Diagrams** — Configure diagram styling, direction, and formats.
   - **Export** — Set the output path and image formats.
   - **Pipeline** — Optionally configure a pre-generation command and check tool availability.
4. Press **F5** or click **Generate** from the Run menu to start generation.
5. Monitor progress in the output panel at the bottom of the window.

---

## Application Shell

The main window follows an IDE-style layout:

- **Top:** Menu bar with File, Run, and Settings menus.
- **Left sidebar:** Navigation panel with collapsible sections for each configuration page.
- **Centre:** Configuration editor for the currently selected navigation item.
- **Bottom:** Output panel displaying real-time generation and analysis logs.

### Menu Bar

| Menu         | Items                                                                                                                                                           |
| ------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **File**     | New Project (`Ctrl+N`), New from Existing, Open Project (`Ctrl+O`), Save (`Ctrl+S`), Save As (`Ctrl+Shift+S`), Close Project (`Ctrl+F4`), Recent Projects, Exit |
| **Run**      | Analyze (`Shift+F5`), Generate (`F5`)                                                                                                                           |
| **Settings** | Open Settings dialog                                                                                                                                            |

### Empty State

When no project is loaded, the centre workspace shows an empty state with the following options:

- **New Project** — Creates a new dependency project from application defaults.
- **New from Existing** — Creates a new project by loading an existing `.sds` file as a starting point, allowing you to clone and modify a configuration.
- **Open Project** — Opens a file browser to select an existing `.sds` file.
- **Recent Projects** — Lists recently opened projects for quick access.
- **Go to Settings** — Opens the application settings dialog.

---

## Navigation Sidebar

The sidebar contains five navigation sections. Each section corresponds to a configuration page. Sections have collapsible cards — cards can be expanded or collapsed independently, and their state is tracked per navigation session (not persisted to disk).

### Project Page

The top-level metadata for your dependency project.

| Field            | Description                                       | Effect on Output                                       |
| ---------------- | ------------------------------------------------- | ------------------------------------------------------ |
| **Project Name** | A friendly name for the dependency project.       | Not used during generation; purely for identification. |
| **Description**  | An optional description of the project's purpose. | Not used during generation; purely for documentation.  |

_Validation:_ Project name must not be empty.

### Solution Page

Configures which solution to analyse, which projects to include or exclude, and per-scope dependency depth.

| Section                     | Field                 | Description                                                                                                                                            | Effect on Output                                                                                           |
| --------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------- |
| **Solution Path**           | Solution Path         | Path to your `.sln` or `.slnx` file. You can browse using the folder button.                                                                           | Determines which solution is parsed. Paths can be stored relative to the `.sds` file or as absolute paths. |
|                             | Use Relative Path     | When enabled, the Browse button stores the path relative to the project file directory.                                                                | Makes the `.sds` file portable across machines.                                                            |
| **Include/Exclude Filters** | Regex To Include      | One or more regex patterns matching projects to process.                                                                                               | Only projects whose paths match any of these patterns are included.                                        |
|                             | Regex To Exclude      | Optional regex patterns to exclude certain projects.                                                                                                   | Matched projects are skipped even if they match an include pattern.                                        |
| **Exclusions**              | Packages To Exclude   | Optional NuGet package IDs to omit from diagrams. Case-insensitive. Transitive dependencies reachable only through excluded packages are also omitted. | Removes specified packages and their transitive chains from diagrams and the dependency summary.           |
|                             | Frameworks To Exclude | Optional framework reference IDs to omit. Case-insensitive.                                                                                            | Removes specified framework references from diagrams.                                                      |
| **Individual Scope**        | Enabled               | Whether to generate per-project diagrams.                                                                                                              | When enabled, one diagram file is produced per matching project showing its dependency graph.              |
|                             | Include Dependencies  | Whether to include framework and package dependencies.                                                                                                 | When disabled, the diagram shows only the project node without its dependencies.                           |
|                             | Transitive Depth      | How many levels of transitive (indirect) packages to show. `0` means no transitive packages.                                                           | Controls how deep the transitive package tree is rendered.                                                 |
| **All Scope**               | Enabled               | Whether to generate a single combined solution diagram.                                                                                                | When enabled, one diagram file is produced showing all matching projects collectively.                     |
|                             | Include Dependencies  | Whether to include framework and package dependencies.                                                                                                 | Same as Individual scope, but for the combined graph.                                                      |
|                             | Transitive Depth      | Transitive depth for the combined graph.                                                                                                               | Same as Individual scope, but for the combined graph.                                                      |

_Validation:_ Solution path must not be empty and must point to an existing file. At least one scope (Individual or All) must be enabled.

### Diagrams Page

Controls how the diagram is styled and which diagram formats are generated.

| Section                | Field              | Description                                                                                                            | Effect on Output                                                                                                                                     |
| ---------------------- | ------------------ | ---------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Direction**          | Direction          | Flow direction of the diagram: `LR` (left-to-right), `RL` (right-to-left), `TB` (top-to-bottom), `BT` (bottom-to-top). | Controls the layout orientation of nodes and edges in generated diagrams.                                                                            |
| **Style - Framework**  | Fill               | CSS hex colour for framework dependency nodes (e.g. `#ECCBC0`).                                                        | Sets the background colour of framework nodes in the diagram.                                                                                        |
|                        | Opacity            | Opacity value between 0.0 and 1.0.                                                                                     | Controls the transparency of framework node fills.                                                                                                   |
| **Style - Package**    | Fill               | CSS hex colour for explicit package dependency nodes.                                                                  | Sets the background colour of package nodes in the diagram.                                                                                          |
|                        | Opacity            | Opacity for package nodes.                                                                                             | Controls transparency of package node fills.                                                                                                         |
| **Style - Transitive** | Fill               | CSS hex colour for transitive (indirect) package dependency nodes.                                                     | Sets the background colour of transitive package nodes, helping them stand out from explicit packages.                                               |
|                        | Opacity            | Opacity for transitive nodes.                                                                                          | Controls transparency of transitive node fills.                                                                                                      |
| **Grouping**           | Enabled            | Whether project and multi-version package grouping containers are rendered.                                            | When enabled, projects are grouped in a visual container and multi-version packages get their own sub-containers. When disabled, all nodes are flat. |
|                        | Background Fill    | CSS hex colour for group container backgrounds.                                                                        | Sets the background colour of grouping containers.                                                                                                   |
|                        | Background Opacity | Opacity for group container backgrounds.                                                                               | Controls transparency of grouping container backgrounds.                                                                                             |
| **Group Identity**     | Group Name         | The display title for the group of projects on the diagram.                                                            | Appears as a heading/label in the diagram.                                                                                                           |
|                        | Group Name Alias   | A short alias used in generated diagram files.                                                                         | A technical identifier for the group container in D2/Mermaid syntax. Not visible in rendered image output.                                           |
| **Formats**            | D2                 | Toggle to enable D2 diagram generation (`.d2` files).                                                                  | When enabled, D2 diagram files are generated for each scope.                                                                                         |
|                        | Mermaid            | Toggle to enable Mermaid diagram generation (`.mmd` files).                                                            | When enabled, Mermaid diagram files are generated for each scope.                                                                                    |

At least one diagram format (D2 or Mermaid) must be selected.

_Validation:_ Fill colours are validated against the `#XXXXXX` hex format (or `#XXX` shorthand).

### Export Page

Controls where and how diagram files and images are saved.

| Field                 | Description                                                                                                                         | Effect on Output                                                                                                                                                                              |
| --------------------- | ----------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Root Path**         | The export root directory. You can browse using the folder button. Relative paths are resolved against the `.sds` file's directory. | Generated diagram files and images are written to sub-folders under this path, organized by target framework and then by renderer (e.g. `<RootPath>/net10.0/d2/`, `<RootPath>/net10.0/mmd/`). |
| **Use Relative Path** | When enabled, the Browse button stores the path relative to the project file directory.                                             | Makes the `.sds` file portable across machines.                                                                                                                                               |
| **Clear Contents**    | When enabled, clears the output sub-folders for configured formats before writing new files.                                        | Prevents stale files from previous runs; only sub-folders for currently configured diagram formats are cleared.                                                                               |
| **Image Formats**     | One or more of: PNG, SVG, PDF. Can be empty for text-only output (`.d2` / `.mmd` files plus the summary).                           | When selected, the corresponding image files are generated from the diagram text files using the D2 CLI and/or Mermaid CLI (mmdc).                                                            |

_Validation:_ Root path must not be empty.

### Pipeline Page

Controls the optional pre-generation command and displays external tool detection status.

#### Pre-Generation Command

| Field                   | Description                                                                              | Effect on Output                                                          |
| ----------------------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| **Enabled**             | When enabled, the pre-generation command runs before diagram generation.                 | Useful for running prerequisites like `dotnet restore` before generation. |
| **Command**             | The executable or script to run (e.g. `dotnet`, `cmd.exe`).                              |                                                                           |
| **Arguments**           | Command-line arguments passed to the command.                                            |                                                                           |
| **Working Directory**   | The working directory for the command. When empty, defaults to the `.sds` file's folder. |                                                                           |
| **Continue on Failure** | When enabled, generation proceeds even if the command exits with an error.               | Allows non-critical steps without blocking generation.                    |

_Validation:_ When enabled, the command field must not be empty.

#### Tool Detection Status

The tool status section shows the availability of external CLI tools required for image export:

| Tool     | Required For                         | Install Link                                                                    |
| -------- | ------------------------------------ | ------------------------------------------------------------------------------- |
| **d2**   | D2 image export (PNG, SVG, PDF)      | [D2 CLI Install](https://github.com/terrastruct/d2/blob/master/docs/INSTALL.md) |
| **mmdc** | Mermaid image export (PNG, SVG, PDF) | [Mermaid CLI](https://github.com/mermaid-js/mermaid-cli)                        |

Each tool entry shows:

- **Tool name** (d2 or mmdc)
- **Status** (Available / Not Found)
- **Resolved path** (executable location when found)
- **Last checked** timestamp
- **Error message** (when not found)

You can click **Rescan Tools** to re-check tool availability without restarting the application. Detection uses a layered resolution: explicit path overrides first (from application settings), then PATH discovery.

**Note:** You can configure dependency projects regardless of tool availability. Missing tools only affect image export capabilities — diagram text files (`.d2`, `.mmd`) and the dependency summary are still generated.

**PNG export notes:** D2's PNG export may have platform-specific requirements. See the [D2 export documentation](https://d2lang.com/ko/tour/exports/) for details on potential issues.

---

## Generation Workflow

### Analysis (Pre-Generation)

Before running full generation, you can run a **dry-run analysis** by pressing `Shift+F5` or selecting **Run > Analyze**.

Analysis performs:

1. Solution parsing and project discovery
2. Project filter evaluation (include/exclude regex)
3. Package and framework dependency resolution
4. Export tool readiness checks

Results are streamed to the output panel, showing:

- All discovered projects
- Which projects are included and excluded (with reasons)
- Tool availability for configured export types

### Generation

Press **F5** or select **Run > Generate** to start full generation.

The generation pipeline is:

1. **Pre-generation command** (if configured and enabled) — executes and streams output in real time
2. **Diagram generation** — project discovery, dependency resolution, framework processing, diagram emission, and optional image export

### During Generation

- The **output panel** shows real-time streaming output
- Configuration editing controls are disabled (menu items, navigation)
- A **Cancel button** is available in the output panel header
- The main window **cannot be closed** while generation is in progress
- The generation status overlay shows "Operation in progress" or "Cancelling…"
- Once cancelled, the Cancel button is disabled immediately

### After Generation

- The output panel shows the completion status and elapsed time
- Success, warning, and failure outcomes are clearly displayed
- The export root path is reported in the output log
- You can open the export folder in Windows File Explorer (available via the output panel or Run menu)

---

## Output Panel

The output panel is docked at the bottom of the main window. It displays all non-artifact output from analysis and generation runs.

### Features

| Feature                    | Description                                                                                                          |
| -------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| **Real-time streaming**    | Messages appear as they are produced during analysis and generation.                                                 |
| **Log level coloring**     | Error (red), Warning (yellow), Information (white), Debug (gray).                                                    |
| **Verbose logging toggle** | When enabled, all application log events (Information level and above) also appear in the output panel. Default: on. |
| **Auto-scroll**            | Automatically scrolls to the bottom when new messages arrive. Default: on.                                           |
| **Word wrap**              | Toggle to wrap long lines (default: off).                                                                            |
| **Clear**                  | Clears all messages from the panel.                                                                                  |
| **Copy All**               | Copies all output panel text to the clipboard.                                                                       |
| **Save As**                | Saves all output panel text to a file via a save dialog.                                                             |
| **Cancel**                 | Cancels the currently running operation (analysis or generation).                                                    |

### Persisted Preferences

Output panel preferences (wrap, verbose logging, auto-scroll) are persisted across sessions in application settings.

---

## Application Settings

Settings are stored in file-based AppData storage (not the Windows registry).

### Default Project Folder

The default folder for Open/Save file dialogs when no project is loaded.

### Tool Path Overrides

Explicit executable paths for external tools when PATH-based discovery is insufficient. Key is the tool name (e.g. `d2`, `mmdc`), value is the full path to the executable.

### Theme

Choose between **Light** and **Dark** themes using MaterialDesignThemes' BundledTheme mechanism. The active theme preference is persisted across restarts. All views render correctly in both themes.

### Log Retention

Number of days to retain rolling log files (default: 30 days). Logs are written to `%APPDATA%\SlnDependencyStudio\Logs\` with the naming pattern `{projectFileBaseName}-{Date}.txt`.

### State Persistence

The following application state is also persisted (separately from user settings):

- **Recent projects** — Recently opened `.sds` file paths, most recent first. If a file no longer exists on disk, it is flagged as missing in the recent projects list.
- **Window placement** — Last-known main window position, size, and state (normal, maximized, minimized).

---

## Keyboard Shortcuts

| Shortcut       | Action            |
| -------------- | ----------------- |
| `Ctrl+N`       | New Project       |
| `Ctrl+O`       | Open Project      |
| `Ctrl+S`       | Save              |
| `Ctrl+Shift+S` | Save As           |
| `Ctrl+F4`      | Close Project     |
| `Shift+F5`     | Analyze (dry run) |
| `F5`           | Generate          |

---

## File Format

SlnDependencyStudio uses `.sds` files (JSON format) for dependency projects. The file format is:

```json
{
  "schemaVersion": 1,
  "metadata": {
    "projectName": "My Project",
    "description": "Description of the project"
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

The `.sds` files produced by the WPF application are fully compatible with the CLI tool and vice versa. Unrecognized JSON fields are preserved when saving to maintain forward compatibility with newer schema versions.

See the [Configuration Reference](./configuration.md) for the complete field-level documentation.
