# SlnDependencyStudio WPF — Implementation Checklist

**PRD:** `PRDs/v4/Studio/SlnDependencyStudio PRD - WPF.md`
**Shared Contracts:** `PRDs/v4/Studio/SlnDependencyStudio PRD - Shared Contracts.md`

---

## ⚠️ Checklist Update Rules

**Do not change the wording of any item when marking it complete.** Do not convert past tense to present tense, do not rephrase, do not add commentary to existing items. Tick the checkbox only. This keeps diffs readable so the human can review and sign off without reading every line that changed.

New items may be added at the end of a phase (with a `(Added: YYYY-MM-DD)` annotation) if the PRD or implementation surfaces a missing requirement, but existing wording must remain untouched.

---

## Phase Status

| Phase | Description                           | Status |
| ----- | ------------------------------------- | ------ |
| 1     | Project Scaffold & Shell Foundation   | ⬜     |
| 2     | Application Settings Service          | ⬜     |
| 3     | Dependency Project Lifecycle          | ⬜     |
| 4     | Metadata & Core Configuration Editing | ⬜     |
| 5     | Advanced Configuration Editing        | ⬜     |
| 6     | Tool Detection & Status               | ⬜     |
| 7     | Pre-Generation Analysis               | ⬜     |
| 8     | Generation Orchestration & Output     | ⬜     |
| 9     | Productivity & Polish                 | ⬜     |
| 10    | Testing                               | ⬜     |
| 11    | Release Automation                    | ⬜     |

---

## Phase 1 — Project Scaffold & Shell Foundation

**Intent:** Create the WPF project on disk with the correct target framework, NuGet dependencies, and project references. Establish the hosting and DI bootstrap using `RxAppBuilder` from ReactiveUI. Wire up `ReactiveWindow<T>` (ReactiveUI.WPF) and `MaterialDesignThemes` for control styling. Build the application shell with three zones — left navigation, centre workspace, and bottom output panel — so all subsequent phases have a place to mount their UI.

### 1.1 Project File & Dependencies

- [ ] 1.1.1 Create the project at `Studio/SlnDependencyStudio.Wpf/SlnDependencyStudio.Wpf.csproj` with `<OutputType>WinExe</OutputType>`, `<UseWPF>true</UseWPF>`, and `<TargetFramework>net10.0-windows10.0.19041</TargetFramework>`.
- [ ] 1.1.2 Add `ProjectReference` to `..\..\Source\SlnDependencyDiagramGenerator.csproj` and `..\SlnDependencyStudio.Shared\SlnDependencyStudio.Shared.csproj`.
- [ ] 1.1.3 Add NuGet packages **only as they are needed per phase** (YAGNI). Do not install all packages up front. When a phase first requires a package, use a **specialised NuGet/Context7 agent** to resolve the latest compatible version for `net10.0-windows10.0.19041`. Phase 1 requires at minimum:
  - `ReactiveUI` and `ReactiveUI.Validation` (MVVM foundation; `RxAppBuilder` is in `ReactiveUI` itself)
  - `ReactiveUI.WPF` (WPF platform bindings for ReactiveUI)
  - `MaterialDesignThemes` (Material Design control theming: Card, PackIcon, Chip, DialogHost)
  - `AllOverIt.ReactiveUI` and `AllOverIt.ReactiveUI.Wpf` (ActivatableViewModel, ViewFactory, ViewRegistry)
  - `AllOverIt.DependencyInjection` (auto-registration, ServiceRegistrarBase, marker interface scanning)
  - `Microsoft.Extensions.Hosting` (hosting and composition)
  - `Serilog.Extensions.Hosting` (Serilog host integration)
  - `Serilog.Sinks.RollingFile` (log file persistence)

  Packages deferred to later phases (see phase heading for when to add):
  - `AllOverIt.Serilog` (CircularBufferSink, ObservableSink) — Phase 8
  - `FluentValidation` (configuration/model validation) — Phase 4
  - `AllOverIt.Validation` and `AllOverIt.Validation.Options` — Phase 4

### 1.2 DI & Hosting Bootstrap

- [x] 1.2.1 Reference the marker interfaces `IStudioScopedDependency`, `IStudioTransientDependency`, and `IStudioSingletonDependency` from `SlnDependencyStudio.Shared.DependencyInjection`. The WPF project's own `DependencyRegistrar` anchor will scan this assembly.
- [x] 1.2.2 Create `DependencyRegistrar : ServiceRegistrarBase` as the assembly anchor for auto-registration scanning.
- [x] 1.2.3 Create an `Extensions/ServiceCollectionExtensions.cs` with an `AddWpfDependencies()` extension method that calls `AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>` with the appropriate filter lambda per the established convention. Defer `AutoRegisterTransient` and `AutoRegisterSingleton` until needed by a specific phase.
- [x] 1.2.4 Create `App.xaml` and `App.xaml.cs`. In `App.xaml`, merge MahApps.Metro and MaterialDesignThemes resource dictionaries.
- [x] 1.2.5 In `Program.cs`, use `RxAppBuilder.CreateReactiveUIBuilder()` followed by `.WithCoreServices()`, `.WithWpf()`, and `.BuildApp()` to bootstrap ReactiveUI v23. Consult the `ViewRegistryDemo` in AllOverIt (`Demos/AllOverIt.ReactiveUI.Wpf/ViewRegistryDemo`) for the canonical startup sequence. The implementing agent should also consult `ReactiveUI` and `ReactiveUI.WPF` native documentation for features that may supersede or complement the AllOverIt helpers.
- [x] 1.2.6 Register core services in the startup pipeline: `AddSlnDependencyGenerator()` (from `SlnDependencyDiagramGenerator.Extensions`), `AddSlnDependencyStudio(validationRegistry)` (from `SlnDependencyStudio.Shared.Extensions`), and `AddWpfDependencies()`.
- [x] 1.2.7 Configure Serilog via the shared `UseStudioSerilog` extension, passing an `ObservableSink` (for live UI output streaming) and a `CircularBufferSink` (for the output panel's history buffer) as frontend-specific sinks. The WPF frontend must provide the log directory from application settings; until settings exist, use a sensible default.

### 1.3 Main Window Shell

- [x] 1.3.1 Create `MainWindow.xaml` as a `metro:MetroWindow` (MahApps.Metro window base). Set title to `"SlnDependencyStudio"`.
- [x] 1.3.2 Create `MainWindowViewModel` extending `ActivatableViewModel` (from `AllOverIt.ReactiveUI`).
- [x] 1.3.3 Register `MainWindow` and `MainWindowViewModel` via DI. Consult the AllOverIt `ViewRegistryDemo` at `Demos/AllOverIt.ReactiveUI.Wpf/ViewRegistryDemo` for guidance: in that demo `MainWindow` is registered as a **Singleton**, while `RegisterWindowTransient<TViewModel, TView>()` is used for non-shell windows. The implementing agent should choose the appropriate lifetime for each registration. Also consult `ReactiveUI` and `ReactiveUI.Wpf` native APIs for view location and registration features that may be more suitable.
- [x] 1.3.4 Layout the shell with three zones using a `Grid`:
  - **Left navigation** (fixed width, ~220px). Placeholder with `TextBlock "Navigation"` and a list of navigation item stubs.
  - **Centre workspace** (star-sized). Placeholder with `TextBlock "Workspace"` bound to a `CurrentView` property on the main view model.
  - **Bottom output panel** (fixed height, ~180px, collapsible). Placeholder with `TextBlock "Output"`.
- [x] 1.3.5 The bottom output panel should be collapsible (a splitter or toggle button). Start collapsed when no generation has run.
- [x] 1.3.6 Wire the `Closing` event to check a `CanClose` observable on the main view model, preventing close during active generation.

> **Direction change (June 2026):** MahApps.Metro and the `MaterialDesignThemes.MahApps` bridge were dropped during Phase 1 implementation. `ReactiveWindow<T>` (ReactiveUI.WPF) cannot inherit from `MetroWindow` (MahApps.Metro) — they share the `Window` base. An attempt to use `MetroWindow` + manual `IViewFor<T>` resolved the inheritance conflict but the title bar was not draggable due to `MaterialDesign3.Defaults.xaml` overriding the MetroWindow style. The stack is now `ReactiveWindow<T>` + `MaterialDesignThemes` for control theming. No PRD-required features depend on MahApps.Metro. See WPF PRD for updated wording.

### 1.4 Navigation & Configuration Experience Design

> **The design below is a SUGGESTION.** The human will review and we will iterate until the navigation grouping, layout pattern, and visual language are agreed. Do not build navigation details until the human signs off.

#### Design Rationale

The JSON config is an implementation detail, not a UX guide. Its nesting (`DiagramGenerator.Projects.Individual.Enabled`) is a serialization concern — the UI should group by _task_, not by object graph. A developer opening this tool thinks: "What solution am I analysing? What output do I want? What should it look like?" Those are the navigation anchors.

The layout pattern is **left nav + tabbed workspace pages** (VS Code Settings style), not a wizard, not an accordion, not vertical tabs. Rationale: configurations are edited non-linearly — the user jumps between solution path, formats, and export path iteratively. A wizard forces a fixed order. An accordion hides context. Vertical tabs waste horizontal real estate on narrow labels. Tabbed workspace pages keep each config domain in its own scrollable surface while the left nav provides persistent top-level orientation.

#### Navigation Structure (5 sections)

The six original sections are consolidated into five, reordered by task flow:

| #   | Nav Item     | Icon (Material Design) | Contents                                                                                                     |
| --- | ------------ | ---------------------- | ------------------------------------------------------------------------------------------------------------ |
| 1   | **Project**  | `FileDocumentOutline`  | Project name, description, file path, dirty state                                                            |
| 2   | **Sources**  | `FolderOpenOutline`    | Solution path, regex include/exclude, package/framework exclusions, per-project + all-projects scope toggles |
| 3   | **Diagrams** | `GraphOutline`         | Format checkboxes (D2/Mermaid), direction, grouping toggle, fill styles                                      |
| 4   | **Export**   | `ExportVariant`        | Output root path, clear-contents toggle, image format checkboxes                                             |
| 5   | **Pipeline** | `Pipe`                 | Pre-generation command config + d2/mmdc tool status (merged)                                                 |

**Key changes from the original suggestion:**

- "Solution" renamed to **Sources** — more precise: it's about where input comes from, not just the `.sln` path.
- "Diagram" → **Diagrams** (plural — you're generating multiple files).
- "Pre-Generation" and "Tools" merged into **Pipeline** — both are about the generation pipeline, not configuration per se. This is where the user configures _what happens before and during_ generation, separated from _what gets generated_.
- **Project** moves to the top — it's the document identity. The user always sees the project name first.

#### Layout Pattern: Card-Based Sections Within Each Page

Each navigation item loads a **scrollable page** in the centre workspace. Within each page, related settings are grouped into **Material Design `Card` controls** with a header, body, and optional footer. This is the same pattern VS Code uses in its Settings editor (settings groups with headings).

```
┌──────────────────────────────────────────────┐
│  ▼ Solution                                │  ← Card header (clickable collapse)
│  ┌────────────────────────────────────────┐ │
│  │  Solution path  [______________] [📂]  │ │  ← Card body
│  │  ⚠ Path does not exist                │ │  ← Inline validation
│  └────────────────────────────────────────┘ │
│                                             │
│  ▼ Project Filters                         │  ← Card header
│  ┌────────────────────────────────────────┐ │
│  │  Include patterns  [______________] [+]│ │
│  │    .*\\.csproj                    [✕] │ │  ← Chip/deletable item
│  │  Exclude patterns  [______________] [+]│ │
│  │    .*(Tests|Studio)\\.           [✕] │ │
│  └────────────────────────────────────────┘ │
│                                             │
│  ▼ Scope                                   │
│  ┌────────────────────────────────────────┐ │
│  │  Per-Project  [✓] Enabled              │ │
│  │    Dependencies [✓]  Depth [==2====]   │ │
│  │  All Projects [✓] Enabled              │ │
│  │    Dependencies [✓]  Depth [==1====]   │ │
│  └────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
```

Cards are independently collapsible via their header click. The collapsed/expanded state persists per navigation session (not saved to file).

#### Progressive Disclosure Strategy

| Tier          | What's in it                                                                | Where it lives                              | Default state                                                            |
| ------------- | --------------------------------------------------------------------------- | ------------------------------------------- | ------------------------------------------------------------------------ |
| **Essential** | Solution path, diagram formats, export root path                            | Top of Sources, Diagrams, Export pages      | Expanded, cards open                                                     |
| **Common**    | Regex patterns, scope toggles, image formats, clear-contents                | Middle of each page                         | Expanded, cards open                                                     |
| **Advanced**  | Fill styles, opacity, grouping, pre-generation command, tool path overrides | Bottom cards on Diagrams and Pipeline pages | **Collapsed by default** with a "Show advanced" label on the card header |

Additionally, the Pipeline nav item itself gets a **subtle "advanced" badge** (Material Design `Chip` with `Info` style, text "optional") because many users will never configure pre-generation commands.

#### Visual Hierarchy

The centre workspace layout within each page follows a **top-to-bottom priority order**:

1. **Path fields** (solution, export root) get a `TextBox` + `Button` row with a file/folder browse icon — these are the most impactful single fields.
2. **Format toggles** are presented as large, labelled `ToggleButton` controls (not small checkboxes) with format icons — the user needs to see at a glance which formats are active.
3. **List editors** (include/exclude patterns, exclusions) use a tag-input pattern: a `TextBox` with an `Add` button, items displayed as removable `Chip` controls below.
4. **Style editors** (fill colours, opacity) use a compact color-picker row — a `TextBox` showing the hex value alongside a small `Rectangle` preview swatch, and a `Slider` for opacity.
5. **Numeric inputs** (transitive depth) use a `Slider` (0-10) with a numeric readout, not a `NumericUpDown` — it's more direct and the range is bounded.

The bottom output panel shows **generation progress** when active and **tool detection status** on idle. A small status bar item in the bottom-right of the window shows d2/mmdc availability as coloured dots (green/orange/red).

#### Validation Integration

Validation is **always visible**, not gated behind a "Validate" button:

- Each card shows a **count badge** on its header when it contains validation errors (e.g., "⚠ 2").
- Individual fields show inline error text below the control via `ReactiveUI.Validation` bindings, styled with Material Design's `MaterialDesignValidationErrorTemplate`.
- The left nav items show a **dot indicator** (orange for warnings, red for errors) next to the nav label — the user can see at a glance which sections need attention.
- A floating validation summary bar was considered but dropped (2026-06-24). The WPF `GridSplitter` cannot respect `MinHeight` constraints when adjacent rows mix `Auto` and `Star` sizing. The nav-item dot indicators already provide the cross-section "which sections have problems" overview, and inline field errors provide the detail — together they deliver equivalent UX without architectural complexity.
- The "Generate" button (in a persistent bottom bar or toolbar) is **disabled** while critical validation errors exist, with a tooltip listing what must be fixed.

#### First Impression — The Empty State

When the app launches with no project loaded, the centre workspace shows an **empty-state landing page**, not a blank grey rectangle:

```
┌──────────────────────────────────────────────────────┐
│                                                      │
│                  🧩  (large icon)                     │
│                                                      │
│            SlnDependencyStudio                       │
│     Dependency diagram generator for .NET            │
│                                                      │
│   ┌─────────────────────┐  ┌──────────────────────┐  │
│   │  📄  New Project    │  │  📂  Open Project    │  │
│   │  Start from defaults│  │  Browse for .sds file│  │
│   └─────────────────────┘  └──────────────────────┘  │
│                                                      │
│   ┌─────────────────────┐  ┌──────────────────────┐  │
│   │  🕐  Recent         │  │  ⚙  Settings         │  │
│   │  MySolution.sds     │  │  Tool paths, logging  │  │
│   └─────────────────────┘  └──────────────────────┘  │
│                                                      │
│   Recent projects (if any):                          │
│     • MySolution.sds              2 days ago         │
│     • LegacyApp.sds               last week          │
│                                                      │
└──────────────────────────────────────────────────────┘
```

The empty state uses Material Design's `Card` + `Icon` + `TextBlock` pattern. The "New Project" and "Open Project" cards are the primary CTAs. Recent projects list below. This immediately communicates capability and makes the JSON-editing user think "I can just open my file and start."

#### Left Navigation Visual Design

The left nav (220px) is a dark sidebar using `MaterialDesignPaper` background with a `Divider` right border. Structure top-to-bottom:

```
┌─────────────────────┐
│  🧩 SlnDep...Studio │  ← App title (small, muted)
│─────────────────────│
│  📋 Project         │  ← Nav items with icon + label
│  📁 Sources      ⚠  │  ← Validation dot on right
│  📊 Diagrams        │
│  📤 Export          │
│  🔗 Pipeline   (opt)│  ← "optional" chip for advanced
│─────────────────────│
│  Recent             │  ← Section header (non-clickable)
│    MySolution.sds   │  ← Recent files (clickable)
│    LegacyApp.sds    │
│─────────────────────│
│  ⚙ Settings         │  ← Bottom-fixed utility items
│─────────────────────│
│  d2 ●  mmdc ●       │  ← Tool status dots (bottom)
└─────────────────────┘
```

The selected nav item gets a left-accent border (4px `MaterialDesignPrimary`) and bold text. Hover gets a subtle background highlight.

#### Actionable Checklist Items

- [x] 1.4.1 Present the five-section navigation design (Project, Sources, Diagrams, Export, Pipeline) to the human for review and iterate until signed off. Confirm the "Pipeline" merge of Pre-Generation + Tools and the "Sources" rename.
- [x] 1.4.2 Agree the progressive disclosure split: Essential (solution path, formats, export root — always expanded), Common (regex patterns, scope toggles, image formats — always expanded), Advanced (fill styles, opacity, grouping, pre-gen, tool paths — collapsed by default with an "Advanced" label). Confirm which fields fall into each tier.
- [x] 1.4.3 Agree the card-based page layout pattern: each navigation section renders a scrollable workspace page with collapsible `Card` controls grouping related settings. Cards show validation error counts on their headers. Confirm this pattern for all five sections.
- [x] 1.4.4 Agree the validation visibility approach: inline field errors via ReactiveUI.Validation, nav-item warning dots, and a disabled Generate button with tooltip. Confirm the error-count badge on card headers. *(Revised 2026-06-24: floating validation summary bar dropped — WPF GridSplitter limitation made it architecturally problematic; nav badges + inline errors provide equivalent coverage.)*
- [x] 1.4.5 Agree the empty-state landing page design: two primary CTA cards (New Project / Open Project), recent projects list, and Settings shortcut. Confirm the empty state is what the user sees on first launch before opening a document.
- [x] 1.4.6 Create `NavigationItemViewModel` with `DisplayName`, `PackIconKind`, `IsSelected`, `HasValidationError`, and `ViewModelType` (for view resolution). Include an `IsAdvanced` flag that controls the "(opt)" chip visibility.
- [x] 1.4.7 Extend `MainWindowViewModel` with a `ReactiveList<NavigationItemViewModel>` bound to the nav `ListBox`, a `CurrentPage` property for the centre workspace, and a `CurrentValidationSummary` collection aggregating cross-section validation state (drives nav-item dot indicators).
- [x] 1.4.8 Implement the left nav `ListBox` with Material Design styling: `MaterialDesignPaper` background, `Divider` border, left-accent selection indicator (4px `MaterialDesignPrimary`), icon+label item template, validation dot template, tool-status row at bottom. The left nav shall include:
  - An app title row at the very top (small, muted text: "🧩 SlnDep...Studio" or similar, truncated if needed).
  - A `DataTemplate` that renders nav items with `IsAdvanced == true` as a Material Design `Chip` with "optional" text beside the label.
  - A hover highlight (subtle background change) on non-selected items.
  - Defer the recent-projects list to Phase 3.
- [x] 1.4.9 Implement card collapse/expand persistence: each collapsible `Card` on a config page remembers its expanded/collapsed state for the duration of the navigation session. When the user switches pages and returns, cards restore their previous state. State is **not** persisted to disk. Use a dictionary keyed by card identifier stored in the page's view model or a shared session-state service.

**Phase 1 completion:** The app launches and displays a themed window with three empty zones. Navigation structure is agreed but detailed views are not yet wired.

---

## Phase 2 — Application Settings Service

**Intent:** Build the `IApplicationSettingsService` and its JSON-file-backed implementation. Create a settings model that stores user preferences (default project folder, tool path overrides, log retention days). Separate durable settings from transient application state (recent files, window position). Add a settings UI so the user can browse for default folders and override tool paths.

### 2.1 Settings Model & Persistence

- [ ] 2.1.1 Create `ApplicationSettings` model with properties: `DefaultProjectFolder` (string), `ToolPathOverrides` (dictionary: tool name → explicit path), `LogRetentionDays` (int, default 30).
- [ ] 2.1.2 Create `IApplicationSettingsService` interface with `LoadAsync()`, `SaveAsync()`, and a `CurrentSettings` property (or observable).
- [ ] 2.1.3 Implement `ApplicationSettingsService` using `System.Text.Json` to persist to `%AppData%/SlnDependencyStudio/settings.json`. Use a **specialised file-I/O agent** if threading or atomic-write concerns arise.
- [ ] 2.1.4 Create `ApplicationState` model (separate from settings) for transient data: `RecentProjects` (list of paths), `WindowPlacement` (left/top/width/height/state). Persist to `%AppData%/SlnDependencyStudio/state.json`.
- [ ] 2.1.5 Register `IApplicationSettingsService` as a singleton in `AddWpfDependencies()`.

### 2.2 Settings UI

- [ ] 2.2.1 Create `SettingsView.xaml` and `SettingsViewModel` with ReactiveUI bindings for `DefaultProjectFolder` (with a `Browse` button that opens `OpenFolderDialog`), tool path overrides (one row per known tool: d2, mmdc, with browse per row), and `LogRetentionDays` (numeric input).
- [ ] 2.2.2 Create `SettingsWindow` (a `ReactiveWindow<T>` with Material Design theming) hosting `SettingsView`.
- [ ] 2.2.3 Add a "Settings" entry in the left navigation that opens the settings window or navigates the workspace to the settings view.
- [ ] 2.2.4 On application startup, load settings. On settings change, save automatically (or provide explicit Save/Cancel).

### 2.3 Log Path Wiring

- [ ] 2.3.1 Ensure the `UseStudioSerilog` call receives the log directory from `IApplicationSettingsService.CurrentSettings`. When no project file is open, use a fallback directory from AppData.
- [ ] 2.3.2 Wire the retention policy: read `LogRetentionDays` from settings and configure the rolling file sink's `retainedFileCountLimit` accordingly.

**Phase 2 completion:** Settings persist across restarts, the user can change defaults and tool paths through a settings dialog, and log files respect the configured retention policy.

---

## Phase 3 — Dependency Project Lifecycle

**Intent:** Implement the full document lifecycle: create from defaults, create by loading an existing `.sds` file, open, save, save-as, close. Track unsaved changes and prompt before discard. Maintain a recent-projects list. Build the empty-state view so first-run users understand what a dependency project is and how to start.

### 3.1 Project Service

- [ ] 3.1.1 Create `IDependencyProjectService` interface with methods: `CreateFromDefaultsAsync()`, `CreateFromExistingAsync(string filePath)`, `OpenAsync(string filePath)`, `SaveAsync(DependencyProjectDocument document, string filePath)`, `SaveAsAsync(DependencyProjectDocument document, string filePath)`.
- [ ] 3.1.2 Implement `DependencyProjectService`. Use `IDependencyProjectSerializer` (already in Shared) for all serialization. Create-from-defaults populates a new `DependencyProjectDocument` with sensible defaults (empty solution path, all formats enabled, etc.).
- [ ] 3.1.3 Register `IDependencyProjectService` as scoped.

### 3.2 Document View Model & Dirty Tracking

- [ ] 3.2.1 Create `DependencyProjectViewModel` wrapping a `DependencyProjectDocument`. Expose `Metadata`, `DiagramGenerator`, and `PreGeneration` as child view models (or direct bindings).
- [ ] 3.2.2 Implement `IsDirty` tracking using ReactiveUI's `WhenAnyValue` on all editable properties. When any bound value changes, set `IsDirty = true`.
- [ ] 3.2.3 Track the current file path (`CurrentFilePath`). `IsNewDocument` is `true` when no file path has been assigned.
- [ ] 3.2.4 Wire `Save` and `SaveAs` commands. `Save` overwrites `CurrentFilePath`; `SaveAs` prompts for a new path and updates `CurrentFilePath`. Both call `IDependencyProjectService` and set `IsDirty = false` on success.
- [ ] 3.2.5 Wire `Open` and `New` commands. Before discarding a dirty document, show a Material Design `DialogHost` confirmation: "Save changes to {project name}?" with Yes/No/Cancel.

### 3.3 Recent Projects

- [ ] 3.3.1 Create `IRecentProjectsService` with `AddAsync(string filePath)`, `GetRecentAsync()`, and `RemoveAsync(string filePath)`.
- [ ] 3.3.2 Implement using the `ApplicationState` model (Phase 2). Store up to 10 recent paths.
- [ ] 3.3.3 In the left navigation, display recent projects below the navigation sections. Each entry shows the file name (no extension) as the display text.
- [ ] 3.3.4 Clicking a recent project entry calls `IDependencyProjectService.OpenAsync()` and navigates to the workspace.

### 3.4 Empty-State / First-Run Experience

- [ ] 3.4.1 When no project is loaded (`CurrentProject == null`), show an empty-state view in the centre workspace. The view should communicate what a dependency project is and offer:
  - **Create from defaults** — calls `IDependencyProjectService.CreateFromDefaultsAsync()` and loads the result.
  - **Create from existing file** — opens a file picker for `.sds` files, then calls `CreateFromExistingAsync()`.
  - **Open recent** — navigates the user to the recent projects list.
  - **Go to settings** — opens the settings dialog.
- [ ] 3.4.2 Use a **specialised WPF/Material Design agent** to style the empty-state view with clear icons, headings, and action buttons suitable for a developer tool.

**Phase 3 completion:** The user can create, open, save, and save-as dependency projects. Unsaved changes are tracked and prompt on close. Recent files are persisted and clickable. The first-run empty state guides new users.

---

## Phase 4 — Metadata & Core Configuration Editing

**Intent:** Build the first configuration editing views. Start with the simplest, most commonly-used settings: project metadata (name, description), solution path, output root, diagram format toggles, image format toggles, and clear-contents. Use ReactiveUI bindings throughout. Introduce inline validation via `ReactiveUI.Validation`.

### 4.1 Metadata Editing

- [ ] 4.1.1 Under the "Project" navigation section, create `ProjectMetadataView.xaml` and `ProjectMetadataViewModel`.
- [ ] 4.1.2 Bind `ProjectName` (TextBox) and `Description` (TextBox, multi-line) to `DependencyProjectMetadata` properties on the current document.
- [ ] 4.1.3 Add a `ReactiveUI.Validation` rule: `ProjectName` must not be empty. Show inline validation error below the TextBox.

### 4.2 Solution & Export Paths

- [ ] 4.2.1 Under the "Sources" navigation section, create the solution-path card: a `TextBox` for solution path and a `Browse` button that opens `OpenFileDialog` filtered to `.sln` and `.slnx` files, per the card-based layout pattern in 1.4.
- [ ] 4.2.2 Under the "Export" navigation section, create the export-root card: a `TextBox` for export root and a `Browse` button that opens `OpenFolderDialog`.
- [ ] 4.2.3 Bind both paths to `DependencyProjectDocument.DiagramGenerator.Projects.SolutionPath` and `.Export.RootPath` respectively.

### 4.3 Format Toggles & Basic Export

- [ ] 4.3.1 In the "Diagrams" navigation section, add large, labelled `ToggleButton` controls (with format icons) for each `DiagramFormat` value (D2, Mermaid), per the visual hierarchy in 1.4. Bind to a collection derived from `DiagramGenerator.Diagram.Formats`. Changes should add/remove from the `Formats` array.
- [ ] 4.3.2 In the "Export" section, add `CheckBox` controls for each `DiagramImageFormat` value (Png, Svg, Pdf). Bind to `DiagramGenerator.Export.ImageFormats`.
- [ ] 4.3.3 Add a `ToggleSwitch` (Material Design styled) for `DiagramGenerator.Export.ClearContents`.
- [ ] 4.3.4 Add validation: at least one diagram format must be selected. The "Generate" button should be disabled until this holds.

### 4.4 Validation Wiring

- [ ] 4.4.1 Wire `ReactiveUI.Validation`'s `BindValidation` helper in each view to display inline error messages with Material Design's `MaterialDesignValidationErrorTemplate`, per the validation integration section in 1.4.
- [x] 4.4.2 ~~Implement the floating validation summary bar~~ — Dropped (2026-06-24). WPF `GridSplitter` limitation: both adjacent rows must use `GridUnitType.Star` for `MinHeight` to be respected, which conflicts with `Auto`-sized validation content. Inline validation + nav badges provide equivalent UX without architectural complexity.
- [ ] 4.4.3 Ensure each card header shows a validation-error count badge (e.g., "⚠ 2") and each left-nav item shows a dot indicator (orange/red) when its page has validation errors, per 1.4.

**Phase 4 completion:** The user can edit metadata, select solution/export paths, toggle formats, and clear contents. Validation errors appear inline and are surfaced in the navigation.

---

## Phase 5 — Advanced Configuration Editing

**Intent:** Build the remaining configuration editors for advanced settings: include/exclude regex patterns, package and framework exclusions, per-scope enablement (individual/all), transitive depth, diagram styling (direction, colors, opacity), grouping, and pre-generation command configuration.

### 5.1 Regex Filters & Exclusions

- [ ] 5.1.1 Under "Sources", add editable list editors for `RegexToInclude` patterns using the tag-input pattern from 1.4: a `TextBox` with an `Add` button, items displayed as removable `Chip` controls below. Bind to an `ObservableCollection<string>`.
- [ ] 5.1.2 Mirror the tag-input pattern for `RegexToExclude`, `PackagesToExclude`, and `FrameworksToExclude`.
- [ ] 5.1.3 Add a **specialised regex agent** review: the UI should provide a tooltip or help text with examples of common regex patterns for project filtering (e.g., `.*Tests.*\.csproj`).

### 5.2 Project Scope Toggles & Transitive Depth

- [ ] 5.2.1 Under "Sources", add `ToggleSwitch` controls for `Individual.Enabled` and `All.Enabled`. When disabled, grey out the associated `IncludeDependencies` checkbox and `TransitiveDepth` slider.
- [ ] 5.2.2 Bind `IncludeDependencies` for each scope to its `CheckBox`.
- [ ] 5.2.3 Bind `TransitiveDepth` to a `Slider` (0–10) with a numeric readout, per the visual hierarchy in 1.4.
- [ ] 5.2.4 Validation: at least one scope (individual or all) must be enabled.

### 5.3 Diagram Styling

- [ ] 5.3.1 Under "Diagrams", add a `ComboBox` for `Direction` with values LR, RL, TB, BT. Display friendly names (Left-to-Right, Right-to-Left, Top-to-Bottom, Bottom-to-Top).
- [ ] 5.3.2 Add style editors for `FrameworkStyle.Fill`, `PackageStyle.Fill`, and `TransitiveStyle.Fill` using the pattern from 1.4: a `TextBox` showing the hex value alongside a small `Rectangle` preview swatch, and a `Slider` for opacity (0.0–1.0).
- [ ] 5.3.3 Add a `ToggleSwitch` for `Grouping.Enabled`. When enabled, show `GroupName`, `GroupNameAlias`, and `Grouping.BackgroundStyle` editors (fill colour swatch + opacity slider).
- [ ] 5.3.4 Bind `GroupName` and `GroupNameAlias` to `TextBox` controls.

### 5.4 Pre-Generation Command

- [ ] 5.4.1 On the "Pipeline" page, add a `ToggleSwitch` for pre-generation `Enabled`. When enabled, show the remaining pre-generation fields within a card. Per the progressive disclosure strategy in 1.4, this card is **collapsed by default** with an "Advanced" label.
- [ ] 5.4.2 Add a `TextBox` for `Command` with a `Browse` button that opens `OpenFileDialog` filtered to `.exe`, `.bat`, `.ps1`, `.cmd`.
- [ ] 5.4.3 Add a `TextBox` for `Arguments`.
- [ ] 5.4.4 Add a `TextBox` for `WorkingDirectory` with a `Browse` button (`OpenFolderDialog`).
- [ ] 5.4.5 Add a `CheckBox` for `ContinueOnFailure` with a tooltip explaining that when checked, generation proceeds even if the pre-generation command fails.
- [ ] 5.4.6 Validation: when `Enabled` is true, `Command` must not be empty.

**Phase 5 completion:** All configuration properties from `DependencyGeneratorConfig` are editable through the UI with inline validation. The configuration surface mirrors the full `.sds` file schema.

---

## Phase 6 — Tool Detection & Status

**Intent:** Integrate with the existing `IToolDetectionService` (in `SlnDependencyDiagramGenerator`) to detect d2 and mmdc availability. Build the tool-status card on the **Pipeline** page (per the 1.4 navigation merge of Pre-Generation + Tools). Show which tools are available, their resolved paths, and allow re-scanning and explicit path overrides. Gate UI options (image formats that depend on missing tools) with explanatory disabled states.

### 6.1 Tool Status Service (WPF Wrapper)

- [ ] 6.1.1 Create a WPF-specific `IToolStatusService` that wraps `IToolDetectionService` and exposes `IObservable<ToolStatus[]>` for d2 and mmdc. The observable should push updates when the user re-scans.
- [ ] 6.1.2 On application startup, call `CheckConfiguredToolsAsync` with no image formats (just to get raw availability). On settings change (tool path overrides updated), re-check with the overrides.

### 6.2 Tool Status View (Pipeline Page Card)

- [ ] 6.2.1 On the "Pipeline" page, create a tool-status card (below the pre-generation card) with `ToolStatusView.xaml` and `ToolStatusViewModel`. Per the 1.4 progressive disclosure, this card is **collapsed by default** with an "Advanced" label.
- [ ] 6.2.2 For each tool (d2, mmdc), display: tool name, icon (green check / red x), availability text ("Found at C:\...\d2.exe" or "Not found"), and a `ReScan` button.
- [ ] 6.2.3 Add a `ReScan All` button that calls `IToolDetectionService.CheckConfiguredToolsAsync` and updates the observable.
- [ ] 6.2.4 Add an explicit path override per tool: a `TextBox` bound to `ApplicationSettings.ToolPathOverrides[toolName]` with a `Browse` button. On change, re-check that tool's availability with the explicit path.
- [ ] 6.2.5 Wire the tool-status dots at the bottom of the left nav (per 1.4 left-nav visual design) to reflect current d2/mmdc availability as coloured dots (green/orange/red).

### 6.3 UI Gating Based on Tool Availability

- [ ] 6.3.1 In the "Export" section, disable and grey out image format checkboxes that require unavailable tools:
  - D2 Png/Svg/Pdf → disabled if d2 is not found.
  - Mermaid Png/Svg → disabled if mmdc is not found.
  - Mermaid Pdf → always disabled (unsupported by mmdc).
- [ ] 6.3.2 Add a tooltip or inline text next to each disabled option explaining why: "d2 CLI not found — install d2 or set an explicit path in Pipeline → Tools".
- [ ] 6.3.3 Tool unavailability must NOT prevent the user from editing or saving a project (FR-7.8). Only generation-time checks should block execution.

**Phase 6 completion:** The tool-status panel shows real-time availability of d2 and mmdc. UI options are gated with clear explanations. Users can override tool paths and re-scan.

---

## Phase 7 — Pre-Generation Analysis

**Intent:** Before generation runs, present an analysis view that shows what the generator will process: all discovered projects, which are included/excluded (with reasons), and tool-readiness for the configured export types. This gives the user confidence before committing to a potentially long generation run.

### 7.1 Analysis Data Model

- [ ] 7.1.1 Create a `PreGenerationAnalysisResult` model with collections: `AllDiscoveredProjects` (name + path), `IncludedProjects`, `ExcludedProjects` (with exclusion reason), and `ToolReadiness` (per-tool availability for configured formats).
- [ ] 7.1.2 If the generator does not yet expose a public API to return project discovery results without running full generation, raise this as an explicit requirement (per FR-6.15) and implement the needed interface before proceeding.

### 7.2 Analysis View

- [ ] 7.2.1 Create `PreGenerationAnalysisView.xaml` and `PreGenerationAnalysisViewModel`.
- [ ] 7.2.2 Display three expandable sections (use Material Design `Expander` or `Card`):
  - **All Projects Discovered** — a scrollable list of project names and their paths.
  - **Included in Generation** — projects that match include patterns and are not excluded.
  - **Excluded from Generation** — projects with the reason (regex exclude, package filter, etc.).
- [ ] 7.2.3 Display a **Tool Readiness** section showing each required tool and its availability status, mirroring the tool-status cards from Phase 6.
- [ ] 7.2.4 Add a "Run Analysis" button on the generation toolbar (or as a pre-generation step). The analysis runs asynchronously and populates the view.

### 7.3 Integration with Generation Flow

- [ ] 7.3.1 The "Generate" command should optionally run analysis first (or have an "Analyze & Generate" button). If analysis returns zero included projects or a missing required tool, block generation and show the analysis view with the problem highlighted.
- [ ] 7.3.2 The analysis view is a configuration aid, not a general-purpose node browser (FR-10.2). Keep it focused and read-only.

**Phase 7 completion:** The user can run a pre-generation analysis to see exactly which projects will be processed and whether the required tools are ready, before committing to generation.

---

## Phase 8 — Generation Orchestration & Output

**Intent:** Build the WPF-specific `IGenerationService` that wraps pre-generation command execution and `IDependencyGenerator.CreateDiagramsAsync`. Wire the "Generate" button. Stream all runtime output to the bottom output panel via Serilog's `ObservableSink`, with color-coded log levels. Disable editing during generation, enable cancellation, and prevent window close.

### 8.1 Generation Service (WPF-Specific)

- [ ] 8.1.1 Create `IWpfGenerationService` with a method `ExecuteAsync(DependencyProjectDocument document, CancellationToken cancellationToken)` that returns an observable stream of `GenerationEvent` (log entries, progress updates, completion/failure).
- [ ] 8.1.2 Implement `WpfGenerationService`. It calls `IPreGenerationCommandRunner.RunAsync()` (from Shared) if pre-gen is enabled, then calls `IDependencyGenerator.CreateDiagramsAsync()`. All log output is captured by the Serilog pipeline and surfaced through the `ObservableSink`.
- [ ] 8.1.3 Support cancellation: the `CancellationToken` is passed to both the pre-generation runner and the generator. On cancel, the service emits a cancellation event.

### 8.2 Generation View & Commands

- [ ] 8.2.1 Add a "Generate" button in the shell toolbar or navigation area. Bind to a `GenerateCommand` on `MainWindowViewModel`.
- [ ] 8.2.2 The `GenerateCommand` should:
  - Validate the current document (both FluentValidation and ReactiveUI.Validation).
  - If validation fails, navigate to the first section with errors and do not start generation.
  - If valid, save the document (if dirty, prompt or auto-save based on preference).
  - Call `IWpfGenerationService.ExecuteAsync()`.
- [ ] 8.2.3 While generation is running (FR-8.7, FR-8.8):
  - Disable the "Generate" button (show a "Cancel" button instead).
  - Disable all configuration editing controls via `CanEdit` observable bound to `IsGenerating`.
  - Prevent window close via `CanClose` observable.
  - Show a progress indicator (Material Design `ProgressBar` with indeterminate mode).
- [ ] 8.2.4 On cancel, call `CancellationTokenSource.Cancel()`, show "Generation cancelled" in the output panel.
- [ ] 8.2.5 On completion, show success or failure in the output panel. Display elapsed time. Show the export root path with a "Open in Explorer" button that calls `IExplorerService`.

### 8.3 Output Panel

- [ ] 8.3.1 Create `OutputPanelView.xaml` and `OutputPanelViewModel`. The view model subscribes to the `ObservableSink` and exposes an `ObservableCollection<LogEntry>`.
- [ ] 8.3.2 Each `LogEntry` has `Timestamp`, `Level`, `Message`, and `SourceContext`. Render entries in a `ListBox` with a `DataTemplate` that color-codes by level: Error=Red, Warning=Yellow, Information=White (or default foreground), Debug=Gray.
- [ ] 8.3.3 Auto-scroll to the bottom as new entries arrive. Use a `ScrollViewer` with `ScrollToBottom` behavior triggered by collection changes.
- [ ] 8.3.4 The output panel retains the full log for the session via the `CircularBufferSink`. Add a "Clear" button to reset the visible log.
- [ ] 8.3.5 The output panel auto-expands when generation starts and can be manually collapsed when generation is idle.
- [ ] 8.3.6 When idle (no generation in progress), the output panel content area displays tool detection status — showing d2 and mmdc availability as coloured dots with resolved paths — per the 1.4 design. This is distinct from the left-nav tool-status dots; it provides a detailed read-only view of the last-known tool check results. When generation starts, this idle content is replaced by the live log stream.

### 8.4 Explorer Service

- [ ] 8.4.1 Create `IExplorerService` with `OpenAsync(string path)`.
- [ ] 8.4.2 Implement using `Process.Start("explorer.exe", path)` on Windows.
- [ ] 8.4.3 After generation completes, display the export root as a clickable hyperlink or button.

**Phase 8 completion:** Generation runs end-to-end with streaming output, cancellation support, UI state gating during active runs, and post-run feedback including Explorer integration.

---

## Phase 9 — Productivity & Polish

**Intent:** Add keyboard shortcuts for common operations. Surface validation status per navigation section. Persist window state across sessions. Polish the UX with loading indicators, confirmation dialogs, and Material Design transitions. Create the documentation-evidence file.

### 9.1 Keyboard Shortcuts

- [ ] 9.1.1 Add `KeyBinding` entries to `MainWindow`:
  - `Ctrl+N` — New project (from defaults)
  - `Ctrl+O` — Open project
  - `Ctrl+S` — Save
  - `Ctrl+Shift+S` — Save As
  - `F5` — Generate
  - `Ctrl+Shift+O` — Open output folder (most recent export root)
  - `Escape` — Cancel generation (when running)
- [ ] 9.1.2 Ensure keyboard shortcuts work regardless of which control has focus (use `InputBindings` at the window level, not control level).

### 9.2 Validation Status in Navigation

- [ ] 9.2.1 Each `NavigationItemViewModel` should expose an `HasErrors` observable derived from the associated view model's validation context.
- [ ] 9.2.2 In the navigation `DataTemplate`, show a red exclamation icon next to sections with errors.

### 9.3 Window State Persistence

- [ ] 9.3.1 On `MainWindow` close, save `Left`, `Top`, `Width`, `Height`, and `WindowState` to `ApplicationState.WindowPlacement`.
- [ ] 9.3.2 On startup, restore the saved window placement. If no saved state exists, center on the primary screen at a default size (1200×800).

### 9.4 Recent Projects in Empty State

- [ ] 9.4.1 When the empty-state view is shown, also display the 5 most recent projects as clickable tiles below the main actions.
- [ ] 9.4.2 If a recent project path no longer exists, show it greyed out with a "file not found" indicator.

### 9.5 Documentation-Evidence File

- [ ] 9.5.1 Create `Studio/SlnDependencyStudio.Wpf/docs/evidence.md` as the documentation-evidence capture file (FR-11.6).
- [ ] 9.5.2 Populate it with implementation notes accumulated during Phases 1–8: service descriptions, key classes, file locations, binding patterns, and architectural decisions.
- [ ] 9.5.3 The evidence file must reference class names, method signatures, and file paths so it can later be transformed into accurate user guides. It should NOT be a user guide itself.

### 9.6 UX Polish

- [ ] 9.6.1 Add subtle animations/transitions when switching navigation sections (Material Design `TransitioningContent` or simple opacity fade).
- [ ] 9.6.2 Add a status bar at the bottom of the window showing: current project name (or "No project"), dirty indicator (•), and last generation timestamp.
- [ ] 9.6.3 Use a **specialised Material Design agent** to review the overall look-and-feel and suggest refinements to spacing, typography, and component selection.

**Phase 9 completion:** The application feels polished and productive. Keyboard shortcuts work globally, validation status is visible at a glance, window state persists, and the documentation-evidence file is populated.

---

## Phase 10 — Testing

**Intent:** Add unit tests for ViewModels and services, integration tests for end-to-end scenarios, and FlaUI-based automated UI tests for navigation, control state, and validation. Tests are deferred to this phase because UI and service design may evolve during Phases 1–9.

### 10.1 Test Project Setup

- [ ] 10.1.1 Create `Tests/Studio/SlnDependencyStudio.Wpf.Tests.Unit/SlnDependencyStudio.Wpf.Tests.Unit.csproj` targeting `net10.0-windows10.0.19041`.
- [ ] 10.1.2 Create `Tests/Studio/SlnDependencyStudio.Wpf.Tests.Integration/SlnDependencyStudio.Wpf.Tests.Integration.csproj` targeting `net10.0-windows10.0.19041`.
- [ ] 10.1.3 Add test NuGet packages: `xunit`, `Shouldly`, `NSubstitute`, `Microsoft.Reactive.Testing` (for observable scheduling).
- [ ] 10.1.4 Add `ProjectReference` to `SlnDependencyStudio.Wpf` from both test projects. Add `InternalsVisibleTo` for both test projects in the WPF `.csproj`.

### 10.2 ViewModel Tests

- [ ] 10.2.1 Test `DependencyProjectViewModel`: dirty tracking (changes set `IsDirty`), save clears dirty, close-with-dirty prompts.
- [ ] 10.2.2 Test `NavigationItemViewModel`: selection changes update `IsSelected`.
- [ ] 10.2.3 Test `ProjectMetadataViewModel`: `ProjectName` empty validation, property change propagation to parent document.
- [ ] 10.2.4 Test configuration view models: format toggles add/remove from collections, path validation, scope enablement gates sub-controls.
- [ ] 10.2.5 Test `ToolStatusViewModel`: re-scan updates observable, explicit path triggers re-check.
- [ ] 10.2.6 Test `OutputPanelViewModel`: log entries appear in collection, auto-scroll trigger fires, clear resets collection.
- [ ] 10.2.7 Test `MainWindowViewModel`: `GenerateCommand` disabled when validation fails, `CanClose` false during generation, cancel triggers cancellation.

### 10.3 Service Tests

- [ ] 10.3.1 Test `ApplicationSettingsService`: save/load roundtrip, default values for missing file, atomic write (no partial writes).
- [ ] 10.3.2 Test `DependencyProjectService`: create from defaults produces a valid `DependencyProjectDocument`, create from existing loads and preserves all fields, save writes valid JSON that can be re-read.
- [ ] 10.3.3 Test `RecentProjectsService`: add pushes to top, duplicate moved to top, max count enforced (10), remove removes.
- [ ] 10.3.4 Test `WpfGenerationService`: pre-gen disabled → skipped, pre-gen failure + continueOnFailure=false → abort, pre-gen failure + continueOnFailure=true → proceed, generator failure → error event, cancellation → cancel event.

### 10.4 Integration Tests

- [ ] 10.4.1 Test full roundtrip: create project → edit all config sections → save → close → re-open → verify all values preserved.
- [ ] 10.4.2 Test shared golden `.sds` files: deserialize with `IDependencyProjectSerializer`, verify the document matches expected structure, serialize back, verify JSON matches baseline.
- [ ] 10.4.3 Test generation parity: run the same `.sds` file through CLI (`SlnDependencyStudio.Cli run --cf ...`) and through WPF `IWpfGenerationService`, verify the output diagram files are identical.
- [ ] 10.4.4 Test settings migration: load a settings file from a previous version, verify it deserializes without errors and new fields get default values.

### 10.5 FlaUI Automated UI Tests

- [ ] 10.5.1 Add `FlaUI.UIA3` NuGet package to the integration test project.
- [ ] 10.5.2 Set `AutomationProperties.AutomationId` on key controls: navigation items, Generate button, Cancel button, output panel, solution path textbox, save button.
- [ ] 10.5.3 Test shell navigation: clicking each nav item switches the centre workspace to the expected view.
- [ ] 10.5.4 Test validation notification: enter invalid data, verify inline error appears and navigation shows error icon.
- [ ] 10.5.5 Test generation UI state: start generation, verify editing controls are disabled, cancel button is enabled, window cannot close. Cancel generation, verify controls re-enable.

**Phase 10 completion:** All unit, integration, and FlaUI tests pass. Coverage focuses on ViewModel logic, service correctness, serialization roundtrips, CLI/WPF parity, and key UI automation paths.

---

## Phase 11 — Release Automation

**Intent:** Create checked-in PowerShell scripts for reliable WPF build and publish workflows. Add parity checks that compare WPF and CLI output for the same `.sds` input. Ensure release outputs are deterministic and tag-ready.

### 11.1 Build & Publish Scripts

- [ ] 11.1.1 Create `Studio/SlnDependencyStudio.Wpf/_build/build.ps1`: builds the WPF project in Release configuration, validates `ProjectReference` mode (no NuGet package reference), runs unit tests.
- [ ] 11.1.2 Create `Studio/SlnDependencyStudio.Wpf/_build/publish.ps1`: publishes the WPF app as a self-contained or framework-dependent single-file executable. The script must be deterministic (same inputs → same outputs).
- [ ] 11.1.3 Both scripts must fail fast on any error and produce clear console output describing what failed.

### 11.2 Parity Checks

- [ ] 11.2.1 Create a parity test script `Studio/_build/parity-check.ps1` that:
  - Runs a shared golden `.sds` file through the CLI.
  - Runs the same file through the WPF app's generation service (via a headless test harness or by invoking the integration test).
  - Compares the output `.d2`/`.mmd` files (and images if tools are available) for bitwise equality.
- [ ] 11.2.2 The parity check script must exit non-zero on any difference and report which files diverged.

### 11.3 Tag Readiness

- [ ] 11.3.1 Document the release process in `Studio/_build/README.md`: steps to tag, build, publish, verify parity, and produce release artifacts.
- [ ] 11.3.2 Verify that a clean `git clone` followed by running the build and publish scripts produces the same outputs as a developer machine.

**Phase 11 completion:** Release builds are scripted, deterministic, and parity-verified. The WPF and CLI frontends are confirmed to produce identical output for the same `.sds` input.

---

## Specialised Agent & Resource Reference

The following table lists phases where a specialised agent or Context7 documentation lookup is recommended. The implementing agent should invoke these before writing relevant code.

| Phase | Area                 | Recommendation                                                                                                                                                              |
| ----- | -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1.1   | NuGet versions       | Context7 lookup for `MaterialDesignThemes`, `ReactiveUI`, `ReactiveUI.WPF`, `AllOverIt.ReactiveUI.Wpf` — resolve latest compatible versions for `net10.0-windows10.0.19041` |
| 1.2   | RxAppBuilder startup | Read `AllOverIt.ReactiveUI.Wpf` demo `ViewRegistryDemo` for the canonical `RxAppBuilder` v23 startup sequence                                                               |
| 1.3   | Window shell         | Context7 lookup for `ReactiveUI.WPF` `ReactiveWindow<T>` features; consult `AllOverIt.ReactiveUI.Wpf` ViewRegistryDemo for window registration pattern                      |
| 1.4   | Navigation UX        | Specialised WPF/Material Design agent for navigation component selection and layout                                                                                         |
| 3.4   | Empty-state design   | Specialised WPF/Material Design agent for developer-tool empty-state UX patterns                                                                                            |
| 5.1   | Regex examples       | Specialised regex agent to provide user-facing help text and examples for project-matching patterns                                                                         |
| 7.2   | Analysis view        | Context7 lookup for `MaterialDesignThemes` `Expander` and `Card` controls                                                                                                   |
| 8.3   | Output panel         | Context7 lookup for `AllOverIt.Serilog` `ObservableSink` and `CircularBufferSink`                                                                                           |
| 9.6   | Polish review        | Specialised Material Design agent for visual consistency, spacing, and typography review                                                                                    |
| 10.5  | FlaUI                | Context7 lookup for `FlaUI.UIA3` — automation element discovery, `AutomationId` usage                                                                                       |

### General Context7 Recommendations

Throughout implementation, if a library's API is unfamiliar, use a **Context7 documentation agent** first before writing code. Key libraries:

- `ReactiveUI` — `WhenAnyValue`, `ReactiveCommand`, `RoutingState`, `ActivatedView`
- `ReactiveUI.WPF` — `ReactiveWindow<T>`, `IViewFor<T>`, view location
- `ReactiveUI.Validation` — `BindValidation`, `ValidationContext`, `ValidationHelper`
- `MaterialDesignThemes` — `Card`, `ColorZone`, `PackIcon`, `DialogHost`, `Chip`
- `AllOverIt.ReactiveUI.Wpf` — `ViewFactory`, `ViewRegistry`, `RegisterWindowTransient<TVM, TV>()`
- `AllOverIt.DependencyInjection` — `ServiceRegistrarBase`, `AutoRegisterScoped`, `AutoRegisterSingleton`, `AutoRegisterTransient`
- `AllOverIt.Serilog` — `ObservableSink`, `CircularBufferSink`
- `FlaUI` — `Application.Attach`, `GetAutomationElement`, `By.AutomationId`

---

## Notes

- Unit and integration tests are in **Phase 10** (intentionally deferred). Manual testing by the human is expected after each phase. Do not add test projects or test files before Phase 10 unless the human explicitly requests them.
- Each phase must be signed off by the human before the next phase begins. The agent should not start Phase N+1 until the human approves Phase N.
- The implementation agent should read the relevant sections of the WPF PRD, Shared Contracts PRD, and existing source files before starting each phase.
