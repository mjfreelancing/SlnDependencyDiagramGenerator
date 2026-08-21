# SlnDependencyStudio WPF User Guide

SlnDependencyStudio WPF is a Windows desktop application for authoring, editing, and running dependency diagram projects. It provides a graphical interface over the `SlnDependencyDiagramGenerator` library, letting you configure solutions, diagram options, export settings, and generation workflows without editing JSON files manually.

**Target platform:** Windows 10+ (`net10.0-windows10.0.19041`)

---

## Table of Contents

- [Quick Start](#quick-start)
- [Application Shell](#application-shell)
- [Empty State](#empty-state)
- [Navigation Sidebar](#navigation-sidebar)
- [Project Page](#project-page)
- [Solution Page](#solution-page)
- [Export Page](#export-page)
- [Diagrams Page](#diagrams-page)
- [Pipeline Page](#pipeline-page)
- [Analyse vs Generate](#analyse-vs-generate)
- [Output Panel](#output-panel)
- [Application Settings](#application-settings)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [File Format](#file-format)
- [Troubleshooting](#troubleshooting)
- [Screenshot Checklist](#screenshot-checklist)

---

## Quick Start

> **Pre-built:** SlnDependencyStudio is distributed as a pre-built Windows application — download the latest release from the [Releases page](https://github.com/mjfreelancing/SlnDependencyDiagramGenerator/releases). Alternatively, build and run it yourself with `dotnet run --project Studio\SlnDependencyStudio.Wpf`.

1. Launch SlnDependencyStudio.
2. On the empty state screen, click **New Project** to create a dependency project from defaults, or **Open Project** to load an existing `.sds` file.
3. Navigate through the sidebar sections to configure your project:
   - **Project** — Set the project name and description.
   - **Solution** — Select your `.sln` or `.slnx` file and configure filters.
   - **Export** — Set the output path and image formats.
   - **Diagrams** — Configure diagram styling, direction, and formats.
   - **Pipeline** — Configure restore, pre/post-generation commands, and check tool availability.
4. Press **F5** or click **Run > Generate** to start generation.
5. Monitor progress in the output panel at the bottom of the window.

---

## Application Shell

The main window follows an IDE-style layout:

- **Top:** Menu bar with **File** and **Run** menus.
- **Left sidebar:** Navigation panel with an item for each configuration page.
- **Centre:** Configuration editor for the currently selected navigation item.
- **Bottom:** Output panel displaying real-time generation and analysis logs.

![Main window — application shell](images/wpf/shell.png)

> 📷 **Screenshot needed:** `shell.png` — the main window with a project open, showing the menu bar, sidebar navigation, a configuration page, and the output panel.

### Menu Bar

| Menu     | Item                   | Shortcut       | Description                                                                   |
| -------- | ---------------------- | -------------- | ----------------------------------------------------------------------------- |
| **File** | **New Project**        | `Ctrl+N`       | Creates a new dependency project from application defaults.                   |
|          | **New from Existing…** | —              | Creates a new project by loading an existing `.sds` file as a starting point. |
|          | **Open Project…**      | `Ctrl+O`       | Opens an existing `.sds` project file.                                        |
|          | **Save**               | `Ctrl+S`       | Saves the current project.                                                    |
|          | **Save As…**           | `Ctrl+Shift+S` | Saves the current project to a new file.                                      |
|          | **Close**              | `Ctrl+F4`      | Closes the current project.                                                   |
|          | **Recent Projects**    | —              | Re-opens a recently opened project from the list.                             |
|          | **Settings…**          | —              | Opens the application settings dialog.                                        |
|          | **Exit**               | `Alt+F4`       | Exits the application.                                                        |
| **Run**  | **Analyse**            | `Shift+F5`     | Runs a pre-flight analysis (dry run) without generating anything.             |
|          | **Generate**           | `F5`           | Runs the full generation pipeline.                                            |

Analyse and Generate are enabled only when a project is open, no operation is running, and there are no validation errors.

While an operation (Analyse or Generate) is running:

- The **Run** menu and all File-menu actions are disabled.
- An overlay appears at the top right showing **Operation in progress** (or **Cancelling…** while cancellation is in progress).
- The main window cannot be closed.

### Closing with Unsaved Changes

Closing the project (`Ctrl+F4`) or the main window (`Alt+F4`) while there are unsaved changes shows a save-before-discard prompt:

![Confirm discard prompt](images/wpf/confirm-discard.png)

> 📷 **Screenshot needed:** `confirm-discard.png` — the confirm-discard prompt shown when closing with unsaved changes.

| Button      | Action                                                  |
| ----------- | ------------------------------------------------------- |
| **Save**    | Saves the project, then closes.                         |
| **Discard** | Discards the unsaved changes and closes without saving. |
| **Cancel**  | Returns to the application without closing.             |

---

## Empty State

When no project is loaded, the centre workspace shows an empty state with the following options:

- **New Project** — Creates a new dependency project from application defaults.
- **New from Existing…** — Creates a new project by loading an existing `.sds` file as a starting point, allowing you to clone and modify a configuration.
- **Open Project** — Opens a file browser to select an existing `.sds` file.
- **Recent Projects** — Lists recently opened projects for quick access. Entries whose files no longer exist are flagged as missing.
- **Settings…** (via the File menu) — Opens the application settings dialog.

![Empty state — no project loaded](images/wpf/empty-state.png)

> 📷 **Screenshot needed:** `empty-state.png` — the empty state with the New Project and Open Project cards and the Recent Projects list.

---

## Navigation Sidebar

The sidebar contains five navigation sections, in this order:

| #   | Section      | Purpose                                            |
| --- | ------------ | -------------------------------------------------- |
| 1   | **Project**  | Project name and description                       |
| 2   | **Solution** | Solution path, filters, exclusions, scopes         |
| 3   | **Export**   | Output path, clear behaviour, image formats        |
| 4   | **Diagrams** | Formats, direction, styling, grouping              |
| 5   | **Pipeline** | Restore, pre/post-generation commands, tool status |

Each navigation item shows two status indicators:

- A **red dot** when the section has validation errors.
- A **hollow ring** when the section has unsaved changes.

The sidebar is hidden automatically when no project is open. Opening a project selects the **Project** page by default.

---

## Project Page

The top-level metadata for your dependency project.

| Field             | Description                                       | Effect on Output                                       |
| ----------------- | ------------------------------------------------- | ------------------------------------------------------ |
| **File location** | The path to the open `.sds` file (read-only).     | —                                                      |
| **Project Name**  | A friendly name for the dependency project.       | Not used during generation; purely for identification. |
| **Description**   | An optional description of the project's purpose. | Not used during generation; purely for documentation.  |

_Validation:_ Project name must not be empty.

![Project page](images/wpf/project.png)

> 📷 **Screenshot needed:** `project.png` — the Project page with a project name and description entered.

---

## Solution Page

Configures which solution to analyse, which projects to include or exclude, and per-scope dependency depth.

| Section                   | Field                | Description                                                                                  | Effect on Output                                                                                           |
| ------------------------- | -------------------- | -------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| **Solution Path**         | Solution Path        | Path to your `.sln` or `.slnx` file. You can browse using the folder button.                 | Determines which solution is parsed. Paths can be stored relative to the `.sds` file or as absolute paths. |
|                           | Use Relative Path    | When enabled, the Browse button stores the path relative to the project file directory.      | Makes the `.sds` file portable across machines.                                                            |
| **Include Patterns**      | Regex To Include     | One or more regex patterns matching projects to process.                                     | Only projects whose paths match any of these patterns are included.                                        |
| **Exclude Patterns**      | Regex To Exclude     | Optional regex patterns to exclude certain projects.                                         | Matched projects are skipped even if they match an include pattern.                                        |
| **Packages to Exclude**   | Package IDs          | Optional NuGet package IDs to omit from diagrams. Case-insensitive.                          | Removes specified packages and their transitive chains from diagrams and the dependency summary.           |
| **Frameworks to Exclude** | Framework IDs        | Optional framework reference IDs to omit. Case-insensitive.                                  | Removes specified framework references from diagrams.                                                      |
| **Individual Scope**      | Enabled              | Whether to generate per-project diagrams.                                                    | When enabled, one diagram file is produced per matching project showing its dependency graph.              |
|                           | Include Dependencies | Whether to include framework and package dependencies.                                       | When disabled, the diagram shows only the project node without its dependencies.                           |
|                           | Transitive Depth     | How many levels of transitive (indirect) packages to show. `0` means no transitive packages. | Controls how deep the transitive package tree is rendered.                                                 |
| **All Scope**             | Enabled              | Whether to generate a single combined solution diagram.                                      | When enabled, one diagram file is produced showing all matching projects collectively.                     |
|                           | Include Dependencies | Whether to include framework and package dependencies.                                       | Same as Individual scope, but for the combined graph.                                                      |
|                           | Transitive Depth     | Transitive depth for the combined graph.                                                     | Same as Individual scope, but for the combined graph.                                                      |

_Validation:_ Solution path must not be empty and must point to an existing file. At least one scope (Individual or All) must be enabled.

![Solution page](images/wpf/solution.png)

> 📷 **Screenshot needed:** `solution.png` — the Solution page with a solution path, some include/exclude patterns, and both scopes configured.

---

## Export Page

Controls where and how diagram files and images are saved.

| Field                   | Description                                                                                                                         | Effect on Output                                                                                                                                                                              |
| ----------------------- | ----------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Export Root**         | The export root directory. You can browse using the folder button. Relative paths are resolved against the `.sds` file's directory. | Generated diagram files and images are written to sub-folders under this path, organised by target framework and then by renderer (e.g. `<RootPath>/net10.0/d2/`, `<RootPath>/net10.0/mmd/`). |
| **Use Relative Path**   | When enabled, the Browse button stores the path relative to the project file directory.                                             | Makes the `.sds` file portable across machines.                                                                                                                                               |
| **Clear Output Folder** | When enabled, clears the output sub-folders for configured formats before writing new files.                                        | Prevents stale files from previous runs; only sub-folders for currently configured diagram formats are cleared.                                                                               |
| **Image Formats**       | One or more of: PNG, SVG, PDF. Can be empty for text-only output (`.d2` / `.mmd` files plus the summary).                           | When selected, the corresponding image files are generated from the diagram text files using the D2 CLI and/or Mermaid CLI (mmdc).                                                            |

_Validation:_ Export root must not be empty.

![Export page](images/wpf/export.png)

> 📷 **Screenshot needed:** `export.png` — the Export page with an export root path and image formats selected.

---

## Diagrams Page

Controls how the diagram is styled and which diagram formats are generated.

| Section                 | Field              | Description                                                                                                            | Effect on Output                                                                                                                                     |
| ----------------------- | ------------------ | ---------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Diagram Formats**     | D2 / Mermaid       | Toggle to enable D2 (`.d2`) and/or Mermaid (`.mmd`) output.                                                            | At least one format must be selected.                                                                                                                |
| **Direction**           | Direction          | Flow direction of the diagram: Left-to-Right (`LR`), Right-to-Left (`RL`), Top-to-Bottom (`TB`), Bottom-to-Top (`BT`). | Controls the layout orientation of nodes and edges in generated diagrams.                                                                            |
| **Styles — Framework**  | Fill               | CSS hex colour for framework dependency nodes (e.g. `#ECCBC0`).                                                        | Sets the background colour of framework nodes in the diagram.                                                                                        |
|                         | Opacity            | Opacity value between 0.0 and 1.0.                                                                                     | Controls the transparency of framework node fills.                                                                                                   |
| **Styles — Package**    | Fill               | CSS hex colour for explicit package dependency nodes.                                                                  | Sets the background colour of package nodes in the diagram.                                                                                          |
|                         | Opacity            | Opacity for package nodes.                                                                                             | Controls transparency of package node fills.                                                                                                         |
| **Styles — Transitive** | Fill               | CSS hex colour for transitive (indirect) package dependency nodes.                                                     | Sets the background colour of transitive package nodes, helping them stand out from explicit packages.                                               |
|                         | Opacity            | Opacity for transitive nodes.                                                                                          | Controls transparency of transitive node fills.                                                                                                      |
| **Grouping**            | Enabled            | Whether project and multi-version package grouping containers are rendered.                                            | When enabled, projects are grouped in a visual container and multi-version packages get their own sub-containers. When disabled, all nodes are flat. |
|                         | Background Fill    | CSS hex colour for group container backgrounds.                                                                        | Sets the background colour of grouping containers.                                                                                                   |
|                         | Background Opacity | Opacity for group container backgrounds.                                                                               | Controls transparency of grouping container backgrounds.                                                                                             |
|                         | Name               | The display title for the group of projects on the diagram.                                                            | Appears as a heading/label in the diagram.                                                                                                           |
|                         | Alias              | A short alias used in generated diagram files.                                                                         | A technical identifier for the group container in D2/Mermaid syntax. Not visible in rendered image output.                                           |

_Validation:_ Fill colours are validated against the `#RRGGBB` hex format (or `#RGB` shorthand).

![Diagrams page](images/wpf/diagrams.png)

> 📷 **Screenshot needed:** `diagrams.png` — the Diagrams page with formats, direction, styles, and grouping configured.

---

## Pipeline Page

The **Diagram Generation Processing Pipeline** page controls the steps that run around diagram generation and shows the status of external tools.

### Restore Solution

| Field                | Description                                                                                                      |
| -------------------- | ---------------------------------------------------------------------------------------------------------------- |
| **Restore Solution** | When enabled (default), runs `dotnet restore` on the solution before generation so project assets are available. |

### Pre-Generation Command

An optional command to run before diagram generation starts.

| Field                   | Description                                                                              |
| ----------------------- | ---------------------------------------------------------------------------------------- |
| **Enabled**             | When enabled, the command runs before generation.                                        |
| **Command**             | The executable or script to run (e.g. `dotnet`, `cmd.exe`). A browse button is provided. |
| **Arguments**           | Command-line arguments passed to the command.                                            |
| **Working Directory**   | The working directory for the command. Empty defaults to the `.sds` file's folder.       |
| **Use Relative Path**   | When enabled, stores the working directory relative to the project file directory.       |
| **Continue on Failure** | When enabled, generation proceeds even if the command exits with an error.               |

_Validation:_ When enabled, the command must not be empty, and the working directory must exist.

### Post-Generation Command

An optional command to run after diagram generation completes. It has the same fields as the pre-generation command (Enabled, Command, Arguments, Working Directory, Use Relative Path) but no **Continue on Failure** option.

### Tool Status

Shows the availability of external CLI tools required for image export:

| Tool     | Required For                         | Install Link                                                          |
| -------- | ------------------------------------ | --------------------------------------------------------------------- |
| **d2**   | D2 image export (PNG, SVG, PDF)      | [D2 CLI Install](https://d2lang.com/tour/install/)                    |
| **mmdc** | Mermaid image export (PNG, SVG, PDF) | [Mermaid CLI](https://github.com/mermaid-js/mermaid-cli#installation) |

Each tool entry shows:

- **Tool name** (d2 or mmdc)
- **Status** (Available / Not Found), e.g. `Found at <path>` or `Not found — install the tool or set a path override`
- **Resolved path** (executable location when found)
- **Last checked** timestamp
- **Error message** (when not found)

Click **Re-scan** to re-check tool availability without restarting the application. A scan also runs automatically when you open the Pipeline page.

> **Note:** You can configure dependency projects regardless of tool availability. Missing tools only affect image export capabilities — diagram text files (`.d2`, `.mmd`) and the dependency summary are still generated.

![Pipeline page](images/wpf/pipeline.png)

> 📷 **Screenshot needed:** `pipeline.png` — the Pipeline page showing the Restore Solution toggle, pre/post-generation sections, and the tool status list with d2/mmdc availability.

---

## Analyse vs Generate

Both actions run on a background thread, stream output to the output panel, and support cancellation via the **Cancel** button or the overlay. If the project has unsaved changes, both save first.

### Analyse (dry run)

Press `Shift+F5` or select **Run > Analyse**. This runs a **pre-flight analysis** that:

1. Validates the configuration (fail-fast on errors).
2. Requires a solution path and discovers projects using the include/exclude regexes (defaulting to `.*\.csproj` when no include pattern is configured).
3. Reports all discovered projects, then classifies them as **included**, **excluded**, or **not matched** (with reasons).
4. Checks **tool readiness** for the configured export types (d2/mmdc), showing ✓/✗ per tool.

Analysis does **not** generate anything and does **not** run any pipeline commands. Use it before Generate to confirm the configuration will behave as expected.

![Analyse (dry run) output](images/wpf/analyse-output.png)

> 📷 **Screenshot needed:** `analyse-output.png` — the output panel after an Analyse run, showing discovered/included/excluded projects and tool readiness.

### Generate

Press `F5` or select **Run > Generate** to run the full pipeline:

1. **Validate** the configuration.
2. **Restore** the solution (if enabled on the Pipeline page).
3. **Pre-generation command** (if enabled) — stops on failure unless Continue on Failure is set.
4. **Diagram generation** — project discovery, dependency resolution, framework processing, diagram emission, and optional image export.
5. **Post-generation command** (if enabled).

After generation completes, the output panel shows the completion status, elapsed time, and the export root path. You can open the export folder in Windows File Explorer from the output panel.

![Generate output](images/wpf/generate-output.png)

> 📷 **Screenshot needed:** `generate-output.png` — the output panel after a Generate run, showing completion status and elapsed time.

---

## Output Panel

The output panel is docked at the bottom of the main window. It displays all non-artifact output from analysis and generation runs.

### Features

| Feature                 | Description                                                                                                                                                                                                                                               |
| ----------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Real-time streaming** | Messages appear as they are produced during analysis and generation.                                                                                                                                                                                      |
| **Log level colouring** | Error (red), Warning (yellow), Information (white), Debug (grey).                                                                                                                                                                                         |
| **Verbose**             | When enabled, Debug-level application log events are also shown in the output panel; when disabled, only Information level and above are shown. This is a display filter only — it does not change what is written to the rolling log file. Default: off. |
| **Wrap**                | Toggle to wrap long lines to the next line. Default: off.                                                                                                                                                                                                 |
| **Auto-scroll**         | Automatically scrolls to the bottom when new messages arrive. Default: on.                                                                                                                                                                                |
| **Cancel**              | Cancels the currently running operation (analysis or generation). Visible only while an operation is running.                                                                                                                                             |
| **Clear**               | Clears all messages from the panel.                                                                                                                                                                                                                       |
| **Copy All**            | Copies all output panel text to the clipboard (`Ctrl+Shift+C`).                                                                                                                                                                                           |
| **Save As…**            | Saves all output panel text to a file via a save dialog.                                                                                                                                                                                                  |

### Persisted Preferences

Output panel preferences (Verbose, Wrap, Auto-scroll) are persisted across sessions in application settings.

---

## Application Settings

Open **File > Settings…** to open the Settings dialog. Settings are stored in file-based AppData storage (not the Windows registry).

| Setting                    | Description                                                                                                                                              |
| -------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Default project folder** | The default folder for Open/Save file dialogs when no project is loaded. Leave empty for no default.                                                     |
| **d2 executable**          | Explicit path to the d2 executable. Auto-detected if empty.                                                                                              |
| **mmdc executable**        | Explicit path to the mmdc executable. Auto-detected if empty.                                                                                            |
| **Log retention (days)**   | Number of days to retain rolling log files (default: 31 days; range 1–90). A restart is required for changes to take effect.                             |
| **Theme**                  | Choose between **Light** and **Dark** themes. The active theme preference is persisted across restarts; changes preview live and are reverted on Cancel. |

- Rolling log files are written to `%APPDATA%\SlnDependencyStudio\Logs\` with the naming pattern `{projectFileBaseName}-{Date}.txt`.
- Rolling logs always capture **all log levels (Debug and above)**, independent of the output panel's Verbose toggle.

![Settings dialog](images/wpf/settings.png)

> 📷 **Screenshot needed:** `settings.png` — the Settings dialog with the default project folder, d2/mmdc paths, log retention, and theme controls.

### State Persistence

The following application state is also persisted (separately from user settings):

- **Recent projects** — Recently opened `.sds` file paths, most recent first, capped at 10. If a file no longer exists on disk, it is flagged as missing in the recent projects list.
- **Window placement** — Last-known main window position, size, and state (normal, maximised, minimised).

---

## Keyboard Shortcuts

| Shortcut       | Action                  |
| -------------- | ----------------------- |
| `Ctrl+N`       | New Project             |
| `Ctrl+O`       | Open Project            |
| `Ctrl+S`       | Save                    |
| `Ctrl+Shift+S` | Save As                 |
| `Ctrl+F4`      | Close Project           |
| `Shift+F5`     | Analyse (dry run)       |
| `F5`           | Generate                |
| `Ctrl+Shift+C` | Copy All (output panel) |
| `Alt+F4`       | Exit                    |

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

The `.sds` files produced by the WPF application are fully compatible with the CLI tool and vice versa. Unrecognised JSON fields are preserved when saving to maintain forward compatibility with newer schema versions.

See the [Configuration Reference](./configuration.md) for the complete field-level documentation.

---

## Troubleshooting

| Symptom                          | Likely cause                                             | Fix                                                                                                                        |
| -------------------------------- | -------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Empty diagrams / no dependencies | Projects have no `obj/project.assets.json`               | Enable **Restore Solution** on the Pipeline page, or run `dotnet restore`/build first.                                     |
| Image formats produce no images  | d2/mmdc not installed                                    | Install the tool, or set an explicit path in **File > Settings…**; check the **Tool Status** section on the Pipeline page. |
| Red dot on a navigation item     | That section has validation errors                       | Open the section and fix the highlighted fields.                                                                           |
| Hollow ring on a navigation item | That section has unsaved changes                         | Save the project (`Ctrl+S`) or discard via the close prompt.                                                               |
| Can't close the window           | An operation is running                                  | Wait for completion or click **Cancel** in the output panel.                                                               |
| Restart banner in Settings       | A restart-sensitive setting (e.g. log retention) changed | Save, restart, and re-open your project.                                                                                   |

---

## Screenshot Checklist

The following screenshots are referenced in this guide. Capture them and drop them into `Docs/images/wpf/` using the file names below — the guide will pick them up automatically.

| #   | File                  | Section                                                       | What to capture                                                          |
| --- | --------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------ |
| 1   | `empty-state.png`     | [Empty State](#empty-state)                                   | No project loaded: New Project + Open Project cards, Recent Projects     |
| 2   | `shell.png`           | [Application Shell](#application-shell)                       | Main window with a project open: menu bar, sidebar, editor, output panel |
| 3   | `project.png`         | [Project Page](#project-page)                                 | Project page with name/description                                       |
| 4   | `solution.png`        | [Solution Page](#solution-page)                               | Solution path, filters, both scopes                                      |
| 5   | `export.png`          | [Export Page](#export-page)                                   | Export root + image formats                                              |
| 6   | `diagrams.png`        | [Diagrams Page](#diagrams-page)                               | Formats, direction, styles, grouping                                     |
| 7   | `pipeline.png`        | [Pipeline Page](#pipeline-page)                               | Restore toggle, pre/post-generation, tool status                         |
| 8   | `analyse-output.png`  | [Analyse vs Generate](#analyse-vs-generate)                   | Output after a dry-run Analyse                                           |
| 9   | `generate-output.png` | [Analyse vs Generate](#analyse-vs-generate)                   | Output after a full Generate                                             |
| 10  | `settings.png`        | [Application Settings](#application-settings)                 | Settings dialog                                                          |
| 11  | `confirm-discard.png` | [Closing with Unsaved Changes](#closing-with-unsaved-changes) | The confirm-discard prompt shown when closing with unsaved changes       |

> **Tip:** The Light theme is the default and is recommended for most screenshots. Add a Dark-theme example (e.g. `settings-dark.png`) if you want to showcase theming.
