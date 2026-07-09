# SlnDependencyStudio WPF — Implementation Checklist

**PRD:** `PRDs/v4/Studio/SlnDependencyStudio PRD - WPF.md`
**Shared Contracts:** `PRDs/v4/Studio/SlnDependencyStudio PRD - Shared Contracts.md`

---

## ⚠️ Checklist Update Rules

**Do not change the wording of any item when marking it complete.** Do not convert past tense to present tense, do not rephrase, do not add commentary to existing items. Tick the checkbox only. This keeps diffs readable so the human can review and sign off without reading every line that changed.

New items may be added at the end of a phase (with a `(Added: YYYY-MM-DD)` annotation) if the PRD or implementation surfaces a missing requirement, but existing wording must remain untouched.

---

## 🗂️ Code Organization Convention (Vertical Slice / Feature-Based)

The WPF project uses a **vertical slice** folder structure under `Features/`. Each feature gets its own folder containing all the code for that feature — interface, implementation, and models. There is no top-level `Services/` or `Models/` folder.

```
Features/
├── <FeatureName>/
│   ├── I<FeatureName>Service.cs       (public interface)
│   ├── <FeatureName>Service.cs       (internal sealed implementation)
│   └── Models/                        (optional — only if the feature has models)
│       └── <ModelName>.cs
```

**Existing features (as of 2026-07-05):**

| Feature Folder             | Namespace                                         | Contents                                                                                                                                                                                       |
| -------------------------- | ------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Features/Application/`    | `SlnDependencyStudio.Wpf.Features.Application`    | `IApplicationSettingsService`, `ApplicationSettingsService`, `Extensions/ApplicationSettingsServiceExtensions`, `Models/` (ApplicationSettings, ApplicationState, WindowPlacement)             |
| `Features/CardSession/`    | `SlnDependencyStudio.Wpf.Features.CardSession`    | `ICardSessionState`, `CardSessionState`                                                                                                                                                        |
| `Features/EmptyState/`     | `SlnDependencyStudio.Wpf.Features.EmptyState`     | `EmptyStateViewModel`, `EmptyStateView`                                                                                                                                                        |
| `Features/ErrorDialog/`    | `SlnDependencyStudio.Wpf.Features.ErrorDialog`    | `IErrorDialogService`, `ErrorDialogService`, `ErrorInfo`                                                                                                                                       |
| `Features/Project/`        | `SlnDependencyStudio.Wpf.Features.Project`        | `IProjectDocumentStore`, `ProjectDocumentStore`, `IProjectMetadataEditor`, `ProjectMetadataEditor`, `ProjectViewModel`, `ProjectView`, `IDependencyProjectService`, `DependencyProjectService` |
| `Features/RecentProjects/` | `SlnDependencyStudio.Wpf.Features.RecentProjects` | `IRecentProjectsService`, `RecentProjectsService`, `Models/` (RecentProjectEntry)                                                                                                              |
| `Features/Settings/`       | `SlnDependencyStudio.Wpf.Features.Settings`       | `SettingsEditorViewModel`, `SettingsEditor`, `SettingsWindowViewModel`, `SettingsWindow`                                                                                                       |
| `Features/Theming/`        | `SlnDependencyStudio.Wpf.Features.Theming`        | `IThemeService`, `ThemeService`                                                                                                                                                                |

> **Note:** `Features/Project/Stores/` is the only feature subfolder beyond `Models/`. The store (`IProjectDocumentStore` / `ProjectDocumentStore`) is a cross-cutting singleton consumed by all page view models and owns the editor instances. Editors live in their respective feature folders (`Solution`, `Export`, `Diagrams`, `Pipeline`), not in `Project/`.

**Rules:**

- Each feature folder is a self-contained vertical slice. Do not scatter a feature's interface, implementation, or models across top-level folders.
- The namespace must match the folder path: `SlnDependencyStudio.Wpf.Features.<FeatureName>` (and `.Models` for the model subfolder).
- Interfaces are `public`. Implementations are `internal sealed` and implement the appropriate DI marker interface (`IStudioSingletonDependency` or `IStudioScopedDependency`) for auto-registration.
- A `Models/` subfolder is created only when the feature has one or more model/POCO classes. If the feature has no models, omit the folder.
- Cross-cutting infrastructure that is not a feature (e.g., `DependencyInjection/`, `Extensions/`, `App.xaml`, `MainWindow.xaml`) remains at the project root.

---

## Phase Status

| Phase | Description                           | Status |
| ----- | ------------------------------------- | ------ |
| 1     | Project Scaffold & Shell Foundation   | ✅     |
| 2     | Application Settings Service          | ✅     |
| 3     | Dependency Project Lifecycle          | ✅     |
| 4     | Metadata & Core Configuration Editing | 🔶     |
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

- [x] 1.1.1 Create the project at `Studio/SlnDependencyStudio.Wpf/SlnDependencyStudio.Wpf.csproj` with `<OutputType>WinExe</OutputType>`, `<UseWPF>true</UseWPF>`, and `<TargetFramework>net10.0-windows10.0.19041</TargetFramework>`.
- [x] 1.1.2 Add `ProjectReference` to `..\..\Source\SlnDependencyDiagramGenerator.csproj` and `..\SlnDependencyStudio.Shared\SlnDependencyStudio.Shared.csproj`.
- [x] 1.1.3 Add NuGet packages **only as they are needed per phase** (YAGNI). Do not install all packages up front. When a phase first requires a package, use a **specialised NuGet/Context7 agent** to resolve the latest compatible version for `net10.0-windows10.0.19041`. Phase 1 requires at minimum:
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
  - **Centre workspace** (star-sized). Placeholder with `TextBlock "Workspace"` bound to a `CurrentPage` property on the main view model.
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

| #   | Nav Item     | Icon (Material Design) | `.sds` node mapped            | Contents                                                                                                     |
| --- | ------------ | ---------------------- | ----------------------------- | ------------------------------------------------------------------------------------------------------------ |
| 1   | **Project**  | `FileDocumentOutline`  | `metadata`                    | Project name, description, file path, dirty state                                                            |
| 2   | **Solution** | `FolderOpenOutline`    | `diagramGenerator.solution`   | Solution path, regex include/exclude, package/framework exclusions, per-project + all-projects scope toggles |
| 3   | **Diagrams** | `GraphOutline`         | `diagramGenerator.diagram`    | Format checkboxes (D2/Mermaid), direction, grouping toggle, fill styles                                      |
| 4   | **Export**   | `ExportVariant`        | `diagramGenerator.export`     | Output root path, clear-contents toggle, image format checkboxes                                             |
| 5   | **Pipeline** | `Pipe`                 | `preGeneration` + tool config | Pre-generation command config + d2/mmdc tool status (merged)                                                 |

**Key changes from the original suggestion:**

- **Solution** is the nav name, matching the `.sds` `diagramGenerator.solution` node (renamed from `projects` in v4.0).
- "Diagram" → **Diagrams** (plural — you're generating multiple files).
- "Pre-Generation" and "Tools" merged into **Pipeline** — both are about the generation pipeline, not configuration per se.
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
| **Essential** | Solution path, diagram formats, export root path                            | Top of Solution, Diagrams, Export pages     | Expanded, cards open                                                     |
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

- [x] 1.4.1 Present the five-section navigation design (Project, Solution, Diagrams, Export, Pipeline) to the human for review and iterate until signed off. Confirm the "Pipeline" merge of Pre-Generation + Tools and the "Solution" rename (from the original "Sources" proposal).
- [x] 1.4.2 Agree the progressive disclosure split: Essential (solution path, formats, export root — always expanded), Common (regex patterns, scope toggles, image formats — always expanded), Advanced (fill styles, opacity, grouping, pre-gen, tool paths — collapsed by default with an "Advanced" label). Confirm which fields fall into each tier.
- [x] 1.4.3 Agree the card-based page layout pattern: each navigation section renders a scrollable workspace page with collapsible `Card` controls grouping related settings. Cards show validation error counts on their headers. Confirm this pattern for all five sections.
- [x] 1.4.4 Agree the validation visibility approach: inline field errors via ReactiveUI.Validation, nav-item warning dots, and a disabled Generate button with tooltip. Confirm the error-count badge on card headers. _(Revised 2026-06-24: floating validation summary bar dropped — WPF GridSplitter limitation made it architecturally problematic; nav badges + inline errors provide equivalent coverage.)_
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

- [x] 2.1.1 Create `ApplicationSettings` model with properties: `DefaultProjectFolder` (string), `ToolPathOverrides` (dictionary: tool name → explicit path), `LogRetentionDays` (int, default 30).
- [x] 2.1.2 Create `IApplicationSettingsService` interface with `LoadAsync()`, `SaveAsync()`, and a `CurrentSettings` property (or observable).
- [x] 2.1.3 Implement `ApplicationSettingsService` using `System.Text.Json` to persist to `%AppData%/SlnDependencyStudio/settings.json`. Use a **specialised file-I/O agent** if threading or atomic-write concerns arise.
- [x] 2.1.4 Create `ApplicationState` model (separate from settings) for transient data: `RecentProjects` (list of paths), `WindowPlacement` (left/top/width/height/state). Persist to `%AppData%/SlnDependencyStudio/state.json`.
- [x] 2.1.5 Register `IApplicationSettingsService` as a singleton in `AddWpfDependencies()`.

### 2.2 Settings UI

- [x] 2.2.1 Create `SettingsView.xaml` and `SettingsViewModel` with ReactiveUI bindings for `DefaultProjectFolder` (with a `Browse` button that opens `OpenFolderDialog`), tool path overrides (one row per known tool: d2, mmdc, with browse per row), and `LogRetentionDays` (numeric input).
- [x] 2.2.2 Create `SettingsWindow` (a `ReactiveWindow<T>` with Material Design theming) hosting `SettingsView`.
- [x] 2.2.3 Add a "Settings" entry in the left navigation that opens the settings window or navigates the workspace to the settings view.
- [x] 2.2.4 On application startup, load settings. On settings change, save automatically (or provide explicit Save/Cancel).

### 2.3 Log Path Wiring

- [x] 2.3.1 Ensure the `UseStudioSerilog` call receives the log directory from `IApplicationSettingsService.CurrentSettings`. When no project file is open, use a fallback directory from AppData.
- [x] 2.3.2 Wire the retention policy: read `LogRetentionDays` from settings and configure the rolling file sink's `retainedFileCountLimit` accordingly.

**Phase 2 completion:** Settings persist across restarts, the user can change defaults and tool paths through a settings dialog, and log files respect the configured retention policy.

---

## Phase 3 — Dependency Project Lifecycle

**Intent:** Implement the full document lifecycle: create from defaults, create by loading an existing `.sds` file, open, save, save-as, close. Track unsaved changes and prompt before discard. Maintain a recent-projects list. Build the empty-state view so first-run users understand what a dependency project is and how to start.

> **Architecture note (2026-07-04):** Phase 3 was refactored to use a centralized **Project Document Store** pattern. The store (`IProjectDocumentStore` / `ProjectDocumentStore`) is a singleton that holds the deserialized `DependencyProjectDocument`, owns **editor wrappers** (one per document sub-object, e.g. `ProjectMetadataEditor` for `DependencyProjectMetadata`), and derives global `IsDirty` from all wrappers. Page view models receive `IProjectDocumentStore` via DI and expose TrackableValues via pass-through properties. This replaces the earlier `DependencyProjectViewModel` + `ConfigureViewModel` + per-page dirty tracking design.

### 3.1 Slice A — Open + Display + Edit

**Goal:** User browses for an `.sds` file → document is loaded → metadata is visible and editable on screen. Pipeline is testable end-to-end before any save/create logic exists.

- [x] 3.1.1 Define `OpenAsync(string filePath)` on `IDependencyProjectService`. Implement by delegating to `IDependencyProjectSerializer`. Register `IDependencyProjectService` as scoped via `IStudioScopedDependency`.
- [x] 3.1.2 Create `IProjectDocumentStore` / `ProjectDocumentStore` under `Features/Project/`. The store is a **singleton** (`IStudioSingletonDependency`) that holds the deserialized `DependencyProjectDocument` privately, exposes `HasDocument` (`[Reactive]`), `CurrentFilePath` (`[Reactive]`), `IsDirty` (`[ObservableAsProperty]` derived from editor wrappers), and `MetadataEditor` (`IProjectMetadataEditor`). Provides `OpenAsync`, `SaveAsync`, `SaveAsAsync`, and `Close` methods. Page view models receive `IProjectDocumentStore` via DI — no `ConfigureViewModel` or document-passing callback is needed.
- [x] 3.1.3 Create `IProjectMetadataEditor` / `ProjectMetadataEditor` under `Features/Project/` — a reactive wrapper for `DependencyProjectMetadata`. Holds `TrackableValue<string>` for `ProjectName` and `Description`, derives its own `IsDirty`, and provides `SetOriginalValues` / `FlushTo` for document ↔ editor mapping. The store owns the concrete instance internally and exposes the interface.
- [x] 3.1.4 Create `TrackableValue<T>` under `Controls/` — a `ReactiveObject` wrapper that tracks whether `Value` has diverged from its original baseline. Uses OAPH for `IsDirty` with a manual `RaisePropertyChanged` seed after `SetOriginalValue` to notify downstream observers. Lives in `Controls/` (shared utility, not feature-specific).
- [x] 3.1.5 Create `ProjectView.xaml` and `ProjectViewModel` under `Features/Project/` — the "Project" nav item page. `ProjectViewModel` is a **plain class** (not `ReactiveObject`) that receives `IProjectDocumentStore` via DI and exposes pass-through properties `ProjectName` and `Description` delegating to `store.MetadataEditor.ProjectName` and `store.MetadataEditor.Description`. XAML binds to `{Binding ProjectName.Value}` etc. Form fields use the reusable `FormField` component from `Controls/`. No `LoadFrom` / `ApplyToDocument` / `MarkClean` methods — the store owns all editing state.
- [x] 3.1.6 Wire the "Open Project" action on `MainWindowViewModel`: calls an `OpenFileDialog` filtered to `.sds` files, then `_store.OpenAsync(filePath)`, and navigates the centre workspace to `ProjectView`. Menu bar added with File → Open Project, Settings, Exit. Nav list enabled/disabled via `HasDocument` property on `MainWindowViewModel` (delegates to `_store.HasDocument`).
- [x] 3.1.7 Verify: load a real `.sds` file → name and description appear in the Project page → edit a field → dirty state updates → UI reflects the change.

### 3.2 Slice B — Save + Dirty Tracking

**Goal:** User can save changes via File menu (Ctrl+S), see dirty state reflected in the window title, and is prompted before discarding unsaved work. No toolbar needed — menu items handle save operations. Dirty tracking is centralized in the store via editor wrappers; no per-page `IsDirty` wiring is needed.

- [x] 3.2.1 Add `SaveAsync(DependencyProjectDocument, string filePath)` to `IDependencyProjectService`. Implement by delegating to `IDependencyProjectSerializer`.
- [x] 3.2.2 Dirty tracking is centralized in `ProjectDocumentStore`:
  - Each editor wrapper (e.g. `ProjectMetadataEditor`) holds `TrackableValue<T>` instances for each editable field and derives its own `IsDirty` via `WhenAnyValue` on the constituent TrackableValues' `IsDirty`.
  - The store's `IsDirty` is an `[ObservableAsProperty]` derived from `MetadataEditor.WhenAnyValue(e => e.IsDirty)`. When future editor wrappers are added, their `IsDirty` values are combined here.
  - `TrackableValue<T>` uses an OAPH for `IsDirty`. `SetOriginalValue` disposes the old subscription, creates a new one, and calls `RaisePropertyChanged(nameof(IsDirty))` to seed the initial value.
  - No `ConfigureViewModel`, no `BindTo` wiring, no per-page `IsXxxPageDirty` slots. Each new page adds its editor wrapper to the store and its `IsDirty` is automatically included.
- [x] 3.2.3 Add `SaveCommand` and `SaveAsCommand` to `MainWindowViewModel`. `SaveCommand.CanExecute` observes `_store.WhenAnyValue(s => s.IsDirty)`. `SaveAsync` delegates to `_store.SaveAsync()` which: (a) calls `FlushTo` on all editor wrappers, (b) serializes via `IDependencyProjectService.SaveAsync`, (c) calls `SetOriginalValues` on all wrappers to mark clean. `SaveAsAsync` additionally updates `CurrentFilePath`. No `ApplyAllPageChanges` or `MarkAllPagesClean` methods — the store owns the full save pipeline.
- [x] 3.2.4 Update the main window title to reflect dirty state and file path. `MainWindow.xaml.cs` subscribes to `_store.WhenAnyValue(store => store.HasDocument)` and `_store.WhenAnyValue(store => store.IsDirty)` (separate subscriptions to avoid `CombineLatest` starvation). `UpdateTitle()` reads `_store.CurrentFilePath` and `_store.IsDirty` imperatively:
  - No project: `"SlnDependencyStudio"`
  - Clean: `"SlnDependencyStudio — MyProject.sds"`
  - Dirty: `"SlnDependencyStudio — MyProject.sds *"`
- [x] 3.2.5 Wire "Before discard" prompt using `DialogHost` (Material Design modal). On File → Open/New/Recent or window close while `_store.IsDirty == true`, show `"Save changes to {project name}?"` with **Save** / **Discard** / **Cancel**. Dialog fires from code-behind. `PromptDiscardAsync()` on `MainWindowViewModel` reads the project name from `_store.MetadataEditor.ProjectName.Value`.
- [x] 3.2.6 Verify: edit name → title shows `*` → File → Save → `*` disappears. Edit name → edit back to original → `*` disappears. Edit name + description → revert description → `*` stays (name still dirty). Close with unsaved changes → dialog appears. Open same file → discard → title shows clean.

### 3.3 Slice C — Create + Recent + Empty State

**Goal:** User can start from defaults or from an existing `.sds` via the empty-state landing page. Recent projects are persisted and clickable.

- [x] 3.3.1 Add `CreateFromDefaults()` to `IDependencyProjectService`. Returns a new `DependencyProjectDocument` with default values. `CreateFromExistingAsync` was intentionally omitted — the save-first-then-open pattern uses the existing `OpenAsync` instead. _(Revised 2026-07-04: save-first approach — New Project and New from Existing both prompt for a save path immediately, write the file, then `OpenAsync`.)_
- [x] 3.3.2 Wire "New Project" command (creates defaults → prompts save dialog → saves → `OpenAsync` into store → navigates to Project page). "New from Existing" prompts for source file → prompts save dialog for destination → saves → `OpenAsync`.
- [x] 3.3.3 Create `IRecentProjectsService` with `Add`, `GetRecent`, `Remove`. Implement using `ApplicationState` (from Phase 2). Store up to 10 recent paths. Registered as `IStudioSingletonDependency` for auto-DI. _(Added 2026-07-05: auto-prunes entries whose files no longer exist on `GetRecent`.)_
- [x] 3.3.4 In the File menu, display recent projects as a submenu below the navigation sections. Each entry shows the file name (no extension). Clicking calls `_store.OpenAsync(filePath)`. Menu disabled when no recent projects exist. _(Revised 2026-07-05: moved from left-nav to File menu — the left nav is for configuration sections only.)_
- [x] 3.3.5 Create `EmptyStateView.xaml` and `EmptyStateViewModel` under `Features/EmptyState/` with: large icon, app title/subtitle, "New Project" card, "Open Project" card (file picker for `.sds`), recent projects list, and Settings shortcut. Use Material Design `Card` + `PackIcon` styling.
- [x] 3.3.6 Wire `MainWindowViewModel` to show `EmptyStateView` when `!_store.HasDocument`. On project load (New or Open), navigate to the Project page.

### 3.4 Slice D — Light/Dark Theme (Added: 2026-07-05)

**Goal:** The user can switch between light and dark themes via the Settings dialog. The preference persists across restarts. All views render correctly in both themes using only `DynamicResource` brushes.

- [x] 3.4.1 Add a `Theme` property to `ApplicationSettings` with values `"Light"` (default) and `"Dark"`. Persist alongside existing settings.
- [x] 3.4.2 In `SettingsView`, add a `ToggleSwitch` for theme selection (Light / Dark). Bind to `ApplicationSettings.Theme`.
- [x] 3.4.3 Implement `IThemeService` with `ApplyTheme(string theme)` that swaps `MaterialDesignThemes.BundledTheme.BaseTheme` at runtime via `Application.Current.Resources.MergedDictionaries`.
- [x] 3.4.4 Call `IThemeService.ApplyTheme` on application startup (from saved preference) and whenever the user changes the theme in settings.
- [x] 3.4.5 Audit all existing views (MainWindow, ProjectView, SettingsView, dialogs) to ensure no hardcoded colour values are used — only `DynamicResource` brushes (e.g., `MaterialDesignBackground`, `MaterialDesignPaper`, `MaterialDesignBody`, `PrimaryHueMidBrush`). Fix any violations found.
- [x] 3.4.6 Verify both themes render correctly: toggle theme → all open windows and dialogs update immediately → restart app → theme preference is restored.
- [x] 3.4.7 If the theme change cannot be applied without a restart, ensure this is reflected in the UI as per LogRetentionDays — **N/A: MaterialDesignThemes supports runtime theme switching via `BundledTheme.BaseTheme`; all views use `DynamicResource` and update immediately. No restart required.**

**Phase 3 completion:** The user can create, open, save, and save-as dependency projects. Unsaved changes are tracked via the centralized store and prompt on close. Recent files are persisted and clickable. The first-run empty state guides new users. Light and dark themes are supported with persisted preference.

---

## Phase 4 — Metadata & Core Configuration Editing

**Intent:** Build the first configuration editing views. Start with the simplest, most commonly-used settings: project metadata (name, description), solution path, output root, diagram format toggles, image format toggles, and clear-contents. Use ReactiveUI bindings throughout. Introduce inline validation via `ReactiveUI.Validation`.

### 4.1 Metadata Editing

- [x] 4.1.1 ~~Create `ProjectMetadataView.xaml` and `ProjectMetadataViewModel`.~~ — Already done as `ProjectView` + `ProjectViewModel` in Phase 3.1.5. `ProjectViewModel` is a plain class that pass-throughs to `IProjectDocumentStore.MetadataEditor`.
- [x] 4.1.2 ~~Bind `ProjectName` and `Description` to `DependencyProjectMetadata`~~ — Already done in 3.1.5 via `TrackableValue<T>` pass-through from the store's `MetadataEditor`. Bindings use `{Binding ProjectName.Value}` and `{Binding Description.Value}`. No `LoadFrom` / `ApplyToDocument` — the store owns editing state and flushes on save.
- [x] 4.1.3 Add a `ReactiveUI.Validation` rule: `ProjectName` must not be empty. Show inline validation error below the TextBox.

  **Implementation notes (2026-07-06):**
  - `ReactiveValidationObject` (ReactiveUI.Validation ≤4.x) is incompatible with Splat ≥19.3.1 — `Splat.IEnableLogger` was removed. Do not use it.
  - Use **ReactiveUI.Validation ≥7.1.0** with `ReactiveObject` + `IValidatableViewModel` + manual `ValidationContext`:

    ```csharp
    public sealed class ProjectViewModel : ReactiveObject, IValidatableViewModel
    {
        public IValidationContext ValidationContext { get; } = new ValidationContext();

        public ProjectViewModel(IProjectDocumentStore store)
        {
            this.ValidationRule(
                vm => vm.ProjectName.Value,
                name => !string.IsNullOrWhiteSpace(name),
                "Project name must not be empty");
        }
    }
    ```

  - In the view code-behind, use `WhenActivated` + `BindValidation` (targeting `FormField.ValidationError`):
    ```csharp
    this.WhenActivated(disposables =>
    {
        this.BindValidation(
                ViewModel,
                vm => vm.ProjectName.Value,
                view => view.ProjectNameFormField.ValidationError)
            .DisposeWith(disposables);
    });
    ```
  - The error is displayed via `FormField.ValidationError` DP (added to `FormField` in 4.1.3). Name the `FormField` instance in XAML (`x:Name="ProjectNameFormField"`) and use: `this.BindValidation(ViewModel, vm => vm.ProjectName.Value, view => view.ProjectNameFormField.ValidationError)`. The error renders inside `FormField` below the separator, aligned with the input column.
  - Requires ReactiveUI ≥23.2.28 (satisfies Validation 7.1.0's dependency).

### 4.2 Solution & Export Paths

**Architecture note:** This phase introduces two new editor wrappers following the `ProjectMetadataEditor` pattern — one per document sub-object — each living in its own feature folder (`Features/Solution/`, `Features/Export/`). The store references editors via their interfaces and coordinates `IsDirty`, `FlushAllEditors`, and `MarkAllEditorsClean` across all editors. Two new navigation items ("Solution", "Export") are added alongside "Project".

#### 4.2.1 GeneratorSolutionOptions Editor

- [x] **Prerequisite:** Rename `Source/Config/GeneratorProjectOptions.cs` → `GeneratorSolutionOptions.cs`; update the `.sds` JSON key `"projects"` → `"solution"` in `DependencyProjectDocument`. Update all references in generator, CLI, serializers, and tests.

- [x] 4.2.1.1 Create `ISolutionOptionsEditor` interface under `Features/Solution/`:

  ```csharp
  public interface ISolutionOptionsEditor
  {
      bool IsDirty { get; }
      TrackableValue<string> SolutionPath { get; }
  }
  ```

  The `SolutionPath` TrackableValue is the only field exposed in this phase. Future phases (5.1, 5.2) add regex patterns, exclusions, and scope toggles to this same interface.

- [x] 4.2.1.2 Create `GeneratorSolutionOptionsEditor` under `Features/Solution/`:
      `internal sealed class GeneratorSolutionOptionsEditor : ReactiveObject, ISolutionOptionsEditor, IDisposable`
  - Initializes `SolutionPath` TrackableValue to `string.Empty` in constructor.
  - Derives `IsDirty` from `SolutionPath.IsDirty` (placeholder for future fields via `CombineLatest`).
  - `SetOriginalValues(GeneratorSolutionOptions source)` — loads `SolutionPath` from source and calls `SetOriginalValue`.
  - `FlushTo(GeneratorSolutionOptions target)` — writes `SolutionPath.Value` to `target.SolutionPath`.

#### 4.2.2 GeneratorExportOptions Editor

- [x] 4.2.2.1 Create `IExportOptionsEditor` interface under `Features/Export/`:

  ```csharp
  public interface IExportOptionsEditor
  {
      bool IsDirty { get; }
      TrackableValue<string> RootPath { get; }
  }
  ```

- [x] 4.2.2.2 Create `GeneratorExportOptionsEditor` under `Features/Export/`:
      `internal sealed class GeneratorExportOptionsEditor : ReactiveObject, IExportOptionsEditor, IDisposable`
  - Same pattern as 4.2.1.2 but wraps `GeneratorExportOptions.RootPath`.

#### 4.2.3 Store Integration

- [x] 4.2.3.1 Add to `IProjectDocumentStore`:

  ```csharp
  ISolutionOptionsEditor SolutionOptionsEditor { get; }
  IExportOptionsEditor ExportOptionsEditor { get; }
  ```

- [x] 4.2.3.2 In `ProjectDocumentStore`:
  - Add `_solutionOptionsEditor` and `_exportOptionsEditor` fields, initialized in constructor.
  - Extend `IsDirty` derivation to combine all three editors:
    ```csharp
    _metadataEditor.WhenAnyValue(e => e.IsDirty)
        .CombineLatest(
            _solutionOptionsEditor.WhenAnyValue(e => e.IsDirty),
            _exportOptionsEditor.WhenAnyValue(e => e.IsDirty),
            (meta, solution, export) => meta || solution || export)
    ```
  - Extend `OpenAsync`: after loading `_metadataEditor`, also load the new editors from `_document.DiagramGenerator.Solution` and `_document.DiagramGenerator.Export`.
  - Extend `FlushAllEditors`: add `_solutionOptionsEditor.FlushTo(_document.DiagramGenerator.Solution)` and `_exportOptionsEditor.FlushTo(_document.DiagramGenerator.Export)`.
  - Extend `MarkAllEditorsClean`: reset the new editors via `SetOriginalValues`.
  - Extend `Close`: reset the new editors to defaults.

#### 4.2.4 Solution Page

- [x] 4.2.4.1 Create `Features/Solution/SolutionViewModel.cs`:
  - Receives `IProjectDocumentStore` via DI (scoped).
  - Inherits from `ReactiveObject`, implements `IValidatableViewModel`.
  - Exposes `SolutionPath` pass-through: `public TrackableValue<string> SolutionPath => _store.SolutionOptionsEditor.SolutionPath;`
  - `ValidationRule`: `SolutionPath.Value` not empty → "Solution path must not be empty."
  - `Interaction<string, string?>` for browse dialog.
  - `ReactiveCommand` that calls `BrowseSolutionPathInteraction.Handle(...)` and assigns result to `SolutionPath.Value`.

- [x] 4.2.4.2 Create `Features/Solution/SolutionView.xaml` + `.xaml.cs`:
  - Card-based layout (header icon `FolderOpenOutline` + title "Solution").
  - Single `FormField` with `x:Name="SolutionPathFormField"`, label "Solution path", containing:
    ```xml
    <Grid>
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
      </Grid.ColumnDefinitions>
      <TextBox Grid.Column="0"
               materialDesign:HintAssist.Hint="Path to .sln or .slnx file"
               Foreground="{DynamicResource MaterialDesign.Brush.Foreground}"
               Text="{Binding SolutionPath.Value, UpdateSourceTrigger=PropertyChanged}" />
      <Button Grid.Column="1" Margin="8,0,0,0" Content="Browse"
              Command="{Binding BrowseSolutionPathCommand}" />
    </Grid>
    ```
  - `WhenActivated` → `BindValidation(view => view.SolutionPathFormField.ValidationError)`.
  - Code-behind registers the `BrowseSolutionPathInteraction` handler to open `OpenFileDialog` filtered to `.sln`/`.slnx`.

- [x] 4.2.4.3 Register `SolutionViewModel`/`SolutionView` via DI in `ServiceCollectionExtensions.AddWpfDependencies()`.

#### 4.2.5 Export Page (Root Path)

- [x] 4.2.5.1 Create `Features/Export/ExportViewModel.cs`:
  - Same pattern as `SolutionViewModel` but binds to `_store.ExportOptionsEditor.RootPath`.
  - `ValidationRule`: `RootPath.Value` not empty → "Export root path must not be empty."
  - `Interaction<string, string?>` for folder browse dialog.

- [x] 4.2.5.2 Create `Features/Export/ExportView.xaml` + `.xaml.cs`:
  - Card-based layout (header icon `ExportVariant` + title "Export").
  - Single `FormField` with `x:Name="ExportRootFormField"`, label "Export root", containing TextBox + folder Browse button.
  - Same `BindValidation` pattern.
  - Code-behind registers the interaction handler to open `OpenFolderDialog`.

- [x] 4.2.5.3 Register `ExportViewModel`/`ExportView` via DI.

#### 4.2.6 Navigation Wiring

- [x] 4.2.6.1 In `MainWindowViewModel` constructor, add navigation items:
  ```csharp
  new NavigationItemViewModel<SolutionViewModel> { DisplayName = "Solution", IconKind = FolderOpenOutline },
  new NavigationItemViewModel<ExportViewModel> { DisplayName = "Export", IconKind = ExportVariant }
  ```

**Phase 4.2 completion:** The user can edit the solution path and export root path through dedicated pages with browse buttons. Both fields support inline validation. Changes are dirty-tracked through the centralized store and flushed on save.

### 4.3 Format Toggles & Basic Export

**Architecture note:** This phase adds a "Diagrams" navigation page and a `DiagramOptionsEditor` under `Features/Diagrams/`. The `ExportOptionsEditor` (in `Features/Export/`) is extended with `ClearContents` and `ImageFormats` TrackableValues.

#### 4.3.1 Diagram Options Editor

- [x] 4.3.1.1 Create `IDiagramOptionsEditor` interface under `Features/Diagrams/`:

  ```csharp
  public interface IDiagramOptionsEditor
  {
      bool IsDirty { get; }
      TrackableValue<ObservableCollection<DiagramFormat>> Formats { get; }
  }
  ```

  The `Formats` collection is an `ObservableCollection<DiagramFormat>` so add/remove operations trigger change detection. In phase 5.3, `Direction`, styles, and grouping are added to this same interface.

- [x] 4.3.1.2 Create `DiagramOptionsEditor` under `Features/Diagrams/`:
      `internal sealed class DiagramOptionsEditor : ReactiveObject, IDiagramOptionsEditor, IDisposable`
  - Initializes `Formats` TrackableValue with an empty `ObservableCollection<DiagramFormat>` in constructor.
  - Derives `IsDirty` from `Formats.IsDirty` (placeholder for future fields via `CombineLatest`).
  - `SetOriginalValues(GeneratorDiagramOptions source)` — populates `Formats` collection from `source.Formats` array, then calls `SetOriginalValue`.
  - `FlushTo(GeneratorDiagramOptions target)` — writes `Formats` collection to `target.Formats` array.
  - **Dirty tracking for collections:** subscribe to `Formats.Value.CollectionChanged` and call `this.RaisePropertyChanged(nameof(IsDirty))` so mutations to the collection (add/remove) propagate to the editor's `IsDirty` OAPH.

- [x] 4.3.1.3 Add `IDiagramOptionsEditor DiagramOptionsEditor { get; }` to `IProjectDocumentStore`. Integrate into `ProjectDocumentStore` (constructor field, `IsDirty` combine, `OpenAsync`, `FlushAllEditors`, `MarkAllEditorsClean`, `Close`).

#### 4.3.2 Extend Export Editor

- [ ] 4.3.2.1 Add `ClearContents` and `ImageFormats` TrackableValues to `IExportOptionsEditor` and `ExportOptionsEditor`:

  ```csharp
  TrackableValue<bool> ClearContents { get; }
  TrackableValue<ObservableCollection<DiagramImageFormat>> ImageFormats { get; }
  ```

- [ ] 4.3.2.2 Update `IsDirty` derivation in `ExportOptionsEditor` to combine `RootPath.IsDirty`, `ClearContents.IsDirty`, and `ImageFormats.IsDirty`.

- [ ] 4.3.2.3 Update `SetOriginalValues` / `FlushTo` for the new fields in `ExportOptionsEditor`.

#### 4.3.3 Diagrams Page

- [x] 4.3.3.1 Create `Features/Diagrams/DiagramsViewModel.cs`:
  - Receives `IProjectDocumentStore` via DI.
  - Inherits from `ReactiveObject`, implements `IValidatableViewModel`.
  - Exposes `Formats` pass-through: `public TrackableValue<ObservableCollection<DiagramFormat>> Formats => _store.DiagramOptionsEditor.Formats;`
  - `ValidationRule`: at least one format must be selected (`Formats.Value.Count > 0`).

- [x] 4.3.3.2 Create `Features/Diagrams/DiagramsView.xaml` + `.xaml.cs`:
  - Card-based layout (header icon `GraphOutline` + title "Diagrams").
  - Card containing two large labelled `ToggleButton` controls for D2 and Mermaid, bound to `Formats.Value` via two-way converters (checked ↔ item in collection).
  - No `FormField` needed — use plain `StackPanel` with `ToggleButton` controls.
  - Named error `TextBlock` (`x:Name="FormatsError"`) below the toggle group for validation.
  - `WhenActivated` → `BindValidation(ViewModel, vm => vm.Formats.Value.Count, view => view.FormatsError.Text)`.

- [x] 4.3.3.3 Register `DiagramsViewModel`/`DiagramsView` via DI.

#### 4.3.4 Extend Export Page

- [ ] 4.3.4.1 In `ExportViewModel`, add pass-through properties for `ClearContents` and `ImageFormats` from `_store.ExportOptionsEditor`.

- [ ] 4.3.4.2 In `ExportView.xaml`, add below the root-path card:
  - Card with `ToggleSwitch` for `ClearContents` (bound to `ClearContents.Value`).
  - Card with `CheckBox` controls for Png, Svg, Pdf (bound to `ImageFormats.Value` via converters).

#### 4.3.5 Navigation Wiring

- [x] 4.3.5.1 In `MainWindowViewModel.NavigationItems`, add:
  ```csharp
  new NavigationItemViewModel<DiagramsViewModel> { DisplayName = "Diagrams", IconKind = GraphOutline }
  ```

**Phase 4.3 completion:** All five nav items are present. Diagrams and Export pages show their complete core settings.

### 4.4 Validation Wiring

- [x] 4.4.1 Wire `ReactiveUI.Validation`'s `BindValidation` helper in each view (Project, Solution, Export, Diagrams) to display inline error messages. For pages using `FormField`, target `view => view.NamedFormField.ValidationError`. For pages without FormField (Diagrams), target a named error `TextBlock`. Reuse the pattern established in 4.1.3: `WhenActivated` → `this.BindValidation(ViewModel, vm => vm.Property, view => view.Element)`.

- [x] 4.4.2 ~~Implement the floating validation summary bar~~ — Dropped (2026-06-24).

- [ ] 4.4.3 Each `NavigationItemViewModel` shall expose `HasValidationError` (already declared). In Phase 9.2, this is wired to observe the page VM's `ValidationContext.IsValid` (inverted) to drive nav-item dot indicators. For now, the property exists and defaults to `false`.

**Phase 4 completion:** The user can edit metadata, solution path, export root, diagram formats, image formats, and clear-contents. Validation errors appear inline. All `.sds` core fields are editable.

---

## Phase 5 — Advanced Configuration Editing

**Intent:** Extend all five editor wrappers with their remaining `.sds` fields. Each editor is extended in-place (no new editor classes) — fields are added to the existing `I*Editor` interfaces and their implementations. This phase completes the full `.sds` configuration surface.

> **Note:** Phase 5 items reference the nav item "Solution" (formerly "Sources") — see Phase 4.2 rename.

### 5.1 Solution Filters & Exclusions (extends `ISolutionOptionsEditor`)

**`.sds` fields covered:** `solution.regexToInclude`, `solution.regexToExclude`, `solution.packagesToExclude`, `solution.frameworksToExclude`

- [ ] 5.1.1 Add four `TrackableValue<ObservableCollection<string>>` properties to `ISolutionOptionsEditor`: `RegexToInclude`, `RegexToExclude`, `PackagesToExclude`, `FrameworksToExclude`.

- [ ] 5.1.2 Update `SolutionOptionsEditor`:
  - Initialize each collection as empty `ObservableCollection<string>` in constructor.
  - Update `IsDirty` derivation to combine all TrackableValues (including `SolutionPath` from 4.2).
  - Update `SetOriginalValues` / `FlushTo` for all four array properties.
  - **Collection dirty tracking:** subscribe to each collection's `CollectionChanged` event in the editor constructor and call `this.RaisePropertyChanged(nameof(IsDirty))` so mutations propagate to the editor's `IsDirty` OAPH. `TrackableValue<T>` itself does not need modification.

- [ ] 5.1.3 In `SolutionView.xaml`, add four `Card` controls (one per list), each with the tag-input pattern from 1.4: a `TextBox` + `Add` button, items displayed as removable Material Design `Chip` controls below. Cards are collapsible via `ICardSessionState`.

- [ ] 5.1.4 Add help tooltips to each list editor with example regex patterns (e.g., `.*Tests.*\.csproj`, `Studio`). Use a specialised regex agent for the example text.

### 5.2 Solution Scope Toggles & Transitive Depth (extends `ISolutionOptionsEditor`)

**`.sds` fields covered:** `solution.individual.{enabled, includeDependencies, transitiveDepth}`, `solution.all.{enabled, includeDependencies, transitiveDepth}`

- [ ] 5.2.1 Add two `TrackableValue<SolutionScopeState>` properties to `ISolutionOptionsEditor`: `IndividualScope` and `AllScope`. Create `Features/Solution/Models/SolutionScopeState.cs` as a POCO with `Enabled` (bool), `IncludeDependencies` (bool), `TransitiveDepth` (int).

- [ ] 5.2.2 Update `GeneratorSolutionOptionsEditor`: map `SolutionScopeState` ↔ `GeneratorSolutionOptions.ProjectScope` in `SetOriginalValues` / `FlushTo`.

- [ ] 5.2.3 In `SolutionView.xaml`, add a "Scope" card with:
  - Two `ToggleSwitch` controls for `IndividualScope.Enabled` and `AllScope.Enabled`.
  - When enabled, show `CheckBox` for `IncludeDependencies` and `Slider` (0–10) for `TransitiveDepth` with numeric readout.
  - When disabled, grey out the dependent controls via `IsEnabled` binding.

- [ ] 5.2.4 Validation: at least one scope must be enabled. Add to `SolutionViewModel.ValidationRule`.

### 5.3 Diagram Styling (extends `IDiagramOptionsEditor`)

**`.sds` fields covered:** `diagram.direction`, `diagram.frameworkStyle`, `diagram.packageStyle`, `diagram.transitiveStyle`, `diagram.groupName`, `diagram.groupNameAlias`, `diagram.grouping.{enabled, backgroundStyle}`

- [ ] 5.3.1 Add TrackableValues to `IDiagramOptionsEditor`: `Direction` (string, one of LR/RL/TB/BT), `FrameworkFill` (string, hex), `FrameworkOpacity` (double), `PackageFill` (string), `PackageOpacity` (double), `TransitiveFill` (string), `TransitiveOpacity` (double), `GroupingEnabled` (bool), `GroupName` (string), `GroupNameAlias` (string), `GroupingFill` (string), `GroupingOpacity` (double).

- [ ] 5.3.2 Update `GeneratorDiagramOptionsEditor`: map TrackableValues ↔ `GeneratorDiagramOptions` sub-objects in `SetOriginalValues` / `FlushTo`. Update `IsDirty` combine.

- [ ] 5.3.3 In `DiagramsView.xaml`, add cards:
  - **Direction** card: `ComboBox` with friendly names (Left-to-Right → LR, etc.).
  - **Styles** card: three rows (Framework, Package, Transitive), each with a `TextBox` showing hex + `Rectangle` preview swatch + `Slider` for opacity (0.0–1.0).
  - **Grouping** card: `ToggleSwitch` for `GroupingEnabled`; when enabled, show `GroupName`/`GroupNameAlias` `TextBox` controls and fill/opacity editors.

### 5.4 Pre-Generation Command (new page section on Pipeline page)

**`.sds` fields covered:** `preGeneration.{enabled, command, arguments, workingDirectory, continueOnFailure}`

- [ ] 5.4.1 Create `Features/Pipeline/PipelineViewModel.cs`:
  - Inherits from `ReactiveObject`, implements `IValidatableViewModel`.
  - Exposes TrackableValue pass-throughs for `Enabled`, `Command`, `Arguments`, `WorkingDirectory`, `ContinueOnFailure` from a `PreGenerationConfigEditor` (see below).

- [ ] 5.4.2 Create `IPreGenerationConfigEditor` interface and `PreGenerationConfigEditor` class under `Features/Pipeline/`:
  - Wraps `PreGenerationConfig` with TrackableValues for all five fields.
  - Add `IPreGenerationConfigEditor PreGenerationEditor { get; }` to `IProjectDocumentStore`.
  - Integrate into `ProjectDocumentStore` (constructor, `IsDirty` combine, open/save/flush/clean/close).

- [ ] 5.4.3 Create `Features/Pipeline/PipelineView.xaml` + `.xaml.cs`:
  - Card-based layout (header icon `Pipe` + title "Pipeline" with an "optional" Chip badge per 1.4).
  - Pre-generation card (collapsed by default via `ICardSessionState`, "Advanced" label):
    - `ToggleSwitch` for `Enabled`; when enabled, show the remaining fields.
    - `TextBox` for `Command` with Browse button (`OpenFileDialog` filtered to `.exe`, `.bat`, `.ps1`, `.cmd`).
    - `TextBox` for `Arguments` and `WorkingDirectory` (with Browse button for folder).
    - `CheckBox` for `ContinueOnFailure` with tooltip.
  - Tool-status card placeholder (filled in Phase 6).

- [ ] 5.4.4 Validation: when `Enabled` is true, `Command` must not be empty.

- [ ] 5.4.5 Register `PipelineViewModel`/`PipelineView` via DI. Add `NavigationItemViewModel<PipelineViewModel>` to `MainWindowViewModel.NavigationItems` with `IsAdvanced = true` (renders the "optional" chip).

**Phase 5 completion:** All `.sds` fields from `metadata`, `solution`, `diagram`, `export`, and `preGeneration` are editable through the UI. Every field has inline validation where appropriate. The configuration surface fully mirrors the `.sds` schema.

---

## Phase 6 — Tool Detection & Status

**Intent:** Integrate with the existing `IToolDetectionService` (in `SlnDependencyDiagramGenerator`) to detect d2 and mmdc availability. Build the tool-status card on the **Pipeline** page. Show which tools are available, their resolved paths, and allow re-scanning and explicit path overrides. Gate UI options that depend on missing tools with explanatory disabled states.

### 6.1 Tool Status Service (WPF Wrapper)

- [ ] 6.1.1 Create `Features/Pipeline/IToolStatusService` under `Features/Pipeline/` (co-located with Pipeline, not a separate feature — tool status is a Pipeline concern):

  ```csharp
  public interface IToolStatusService : IStudioSingletonDependency
  {
      IObservable<IReadOnlyList<ToolStatusEntry>> ToolStatuses { get; }
      Task RescanAsync(CancellationToken ct);
  }
  ```

  `ToolStatusEntry` has `ToolName`, `IsAvailable`, `ResolvedPath`, `LastChecked`.

- [ ] 6.1.2 Implement `ToolStatusService` wrapping `IToolDetectionService.CheckConfiguredToolsAsync()`. On construction, run an initial scan. On `RescanAsync`, update the observable.

- [ ] 6.1.3 Register `IToolStatusService` as a singleton. On app startup, the service initializes itself. On settings change (tool path overrides updated via Phase 5.4), call `RescanAsync`.

### 6.2 Tool Status View (Pipeline Page Card)

- [ ] 6.2.1 In `Features/Pipeline/PipelineView.xaml`, add a tool-status `Card` below the pre-generation card. Per 1.4 progressive disclosure, this card is **collapsed by default** with an "Advanced" label.

- [ ] 6.2.2 For each tool (d2, mmdc), display: tool name, icon (green `CheckCircle` / red `CloseCircle`), availability text ("Found at C:\...\d2.exe" or "Not found — install d2 or set path override"), and a `ReScan` button.

- [ ] 6.2.3 Add a `ReScan All` button that calls `IToolStatusService.RescanAsync()`.

- [ ] 6.2.4 Add an explicit path override per tool: a `TextBox` bound to `ApplicationSettings.ToolPathOverrides[toolName]` with a `Browse` button. On change, call `RescanAsync`.

- [ ] 6.2.5 Wire the tool-status dots at the bottom of the left nav (per 1.4 visual design) to observe `IToolStatusService.ToolStatuses` and render green/orange/red dots.

### 6.3 UI Gating Based on Tool Availability

- [ ] 6.3.1 In `ExportView`, observe `IToolStatusService.ToolStatuses` and disable/grey out image format checkboxes that depend on unavailable tools:
  - D2-dependent formats (Png, Svg, Pdf) → disabled if d2 not found.
  - Mermaid-dependent formats (Png, Svg) → disabled if mmdc not found.
  - Mermaid Pdf → always disabled (not supported by mmdc).

- [ ] 6.3.2 Add tooltip or inline text next to each disabled option explaining why (e.g., "d2 CLI not found — install d2 or set path in Pipeline → Tools").

- [ ] 6.3.3 Per FR-7.8, tool unavailability must NOT prevent editing or saving a project. Gating is UI-only, not enforced at the model/serialization level.

**Phase 6 completion:** Tool status is visible on the Pipeline page and left-nav dots. Export options are gated with explanations. Users can override tool paths and re-scan.

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

- [x] 9.3.1 On `MainWindow` close, save `Left`, `Top`, `Width`, `Height`, and `WindowState` to `ApplicationState.WindowPlacement`.
- [x] 9.3.2 On startup, restore the saved window placement. If no saved state exists, center on the primary screen at a default size (1200×800).

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

- [ ] 10.2.1 Test `ProjectDocumentStore`: dirty tracking (edits set `IsDirty`), save clears dirty via `MarkAllEditorsClean`, close-with-dirty prompts, `OpenAsync` loads and populates editors.
- [ ] 10.2.2 Test `NavigationItemViewModel`: selection changes update `IsSelected`.
- [ ] 10.2.3 Test `ProjectMetadataEditor`: `SetOriginalValues` populates TrackableValues and marks clean, `FlushTo` writes back, `IsDirty` derived correctly from constituent TrackableValues.
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
