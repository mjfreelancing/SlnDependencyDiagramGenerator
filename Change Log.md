# v4.0.0

## 23 Aug 2026

### SlnDependencyDiagramGenerator

- Added support for `.slnx` (XML-based solution) files.
- Added support for excluding framework references from diagrams and the dependency summary.
- Improved multi-version package conflict reporting in the dependency summary.
- Added project grouping options and a renderer-neutral intermediate representation.
- Added a Mermaid (`.mmd`) renderer alongside D2, with multi-format output via a `formats` array (D2 and/or Mermaid).
- Added cancellation support to the generator.
- Normalised generated file names to safe lower-kebab-case and made badge colours deterministic.
- Added unit and integration test projects for the generator, validators, and renderers.

### SlnDependencyStudio v1.0.0 (new)

- **SlnDependencyStudio.Shared** — Shared `.sds` document format, services, and process-execution contracts used by both frontends.
- **SlnDependencyStudio CLI** — A cross-platform command-line tool with `validate` and `run` commands, deterministic exit codes (1001–1014, 1999), verbose logging, and rolling file logs.
- **SlnDependencyStudio WPF** — A Windows desktop application (ReactiveUI, MaterialDesignThemes) with an IDE-style shell, five configuration pages, Analyse/Generate workflows, an output panel, application settings, and light/dark theming.
  - Added an empty state with New/Open/Recent actions and a recent-projects list (most recent first, capped at 10, removable entries, missing-file detection).
  - Added per-page dirty tracking and validation indicators on navigation items and the window title.
  - Added an output panel with real-time streaming, level colouring, verbose/wrap/auto-scroll toggles, cancel, clear, copy-all, and save-as (preferences persisted).
  - Added application settings for the default project folder, d2/mmdc executable overrides, log retention, and theme.
  - Added persistent window placement and rolling file logs.
  - Added Save As relative-path re-basing so portable `.sds` files remain valid when moved.
  - Added a shared error dialog service.
- Added solution restore and optional pre/post-generation command support to the generation pipeline (CLI and WPF).
- Added tool detection for the d2 and mmdc executables.
- Added an installer project for the Studio applications.
- Added unit and integration test projects for the Studio CLI, Shared, and WPF applications.

---

# v3.0.0

## 26 Nov 2025

- Added support for NET 10 while retaining NET 9 and NET 8 targets.
- Updated core dependencies to newer major versions (including AllOverIt 9.0.0 and NuGet.Protocol 7.0.1).
- Updated framework-specific Microsoft.Build package selection for multi-targeting.
- Improved solution path validation by resolving full paths and returning clearer not found messages.
- Updated sample usage to handle validation exceptions and refreshed README framework/output references.
- Removed committed net8.0 sample output artifacts.

---

# v2.1.0

## 24 Mar 2025

- Handle package versions such as [1.0.0] so they are processed as 1.0.0.

---

# v2.0.0

## 26 Nov 2024

- Updated to latest dependencies
- Support changed to NET 8 and NET 9

---

# v1.5.1

## 27 Jul 2024

- Fixed regression where not all dependencies were being processed.

---

# v1.5.0

## 27 Jul 2024

- Skip projects that do not have a configured target framework.
- Only create folders for target frameworks processed.

---

# v1.4.0

## 25 Jun 2024

- Package dependency updates.

---

# v1.3.0

## 12 Jun 2024

- Package dependency updates.

---

# v1.2.0

## 20 May 2024

- Added a regex option to exclude projects.

---

# v1.1.1

## 16 May 2024

- Package dependency updates.

---

# v1.1.0

## 06 Apr 2024

- Updated to recognise versioned windows-based framework targets.

---

# v1.0.1

## 24 Feb 2024

- Maintenance package and documentation updates.

---

# v1.0.0

## 09 Feb 2024

- Initial release.
