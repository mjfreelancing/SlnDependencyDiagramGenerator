# Product Requirements Document - SlnDependencyStudio CLI

**Date:** May 2026  
**Status:** Draft  
**Scope:** Cross-platform CLI application for running dependency-diagram projects produced by SlnDependencyStudio WPF, backed by `SlnDependencyDiagramGenerator`, with shared contracts aligned to `PRDs/v4/Studio/SlnDependencyStudio PRD - Shared Contracts.md` and `PRDs/v4/Studio/SlnDependencyStudio PRD - WPF.md`

---

## 1. Executive Summary

`SlnDependencyStudio CLI` is a headless command-line frontend that executes dependency-project files and runs the same generation pipeline used by the WPF application. It exists so generation can be automated in scripts, CI workflows, and developer terminals while preserving parity with desktop workflows.

The CLI is a first-class deliverable in the same release train as WPF. Shared document contracts, orchestration behavior, cancellation semantics, and dependency-mode discipline must remain consistent across both frontends.

The CLI should target cross-platform execution.

---

## 2. Product Goals

### Goals

1. Provide a cross-platform CLI to run dependency-project files without using the WPF UI.
2. Treat CLI and WPF as first-class artifacts for the same release train.
3. Guarantee shared dependency-project contract parity with WPF.
4. Reuse shared orchestration and generation pipeline services instead of duplicating logic in the CLI host.
5. Support deterministic automation-friendly behavior: clear exit codes, predictable stdout/stderr, reproducible runs.
6. Support user-initiated cancellation and fail-fast handling for long-running generation tasks.
7. Use `ProjectReference` to `SlnDependencyDiagramGenerator` for both CLI and WPF frontends for all builds (development and release).
8. Support release automation through checked-in PowerShell scripts to reduce tag/release surprises.
9. Keep cross-platform compatibility as a first-class constraint for CLI and shared code.
10. Prepare packaging considerations when command surface and compatibility are stable.
11. Maintain a structured documentation-evidence artifact to capture implementation details that can later be transformed into accurate user guides.
12. Prioritize PRD and checklist maintenance during implementation, with user-guide authoring treated as a secondary end-phase focus.

### Non-Goals

1. Replacing the underlying `SlnDependencyDiagramGenerator` engine.
2. Re-implementing WPF authoring UX in CLI.
3. Building a separate, frontend-specific dependency-project format.
4. Introducing shell-specific business rules that differ from WPF.

---

## 3. Target Users

Primary users are developers and build pipelines that need to execute dependency generation from terminals or automation systems, often without UI access. Secondary users are WPF users who want scripted or CI-based runs using the same dependency-project files.

---

## 4. Product Overview

The CLI frontend manages these concerns:

1. Load and validate dependency-project files created by WPF or other trusted producers.
2. Execute optional pre-generation command and generation pipeline with shared orchestration semantics.
3. Stream run output and return deterministic process exit codes.
4. Support tool detection (d2/mermaid in the first release) and explicit path overrides in non-interactive environments.

The CLI shall consume shared contracts and services that are also consumed by WPF so behavior remains aligned.

---

## 5. Functional Requirements

### FR-1: CLI Host and Command Surface

| ID      | Requirement                                                                                                                                                                                                                                                 |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-1.1  | The application shall be a cross-platform .NET console application.                                                                                                                                                                                         |
| FR-1.2  | The CLI shall accept a dependency-project file path as primary input.                                                                                                                                                                                       |
| FR-1.3  | The CLI shall provide a command to validate a dependency-project file without running generation.                                                                                                                                                           |
| FR-1.4  | The CLI shall provide a command to execute generation from a dependency-project file.                                                                                                                                                                       |
| FR-1.5  | The CLI shall log resolved paths and key options at Information level during `run` and `validate` commands so logs can be used for diagnostics and troubleshooting. A dedicated `config-print` command is deferred to a future release.                     |
| FR-1.6  | The CLI shall support non-interactive operation suitable for CI environments.                                                                                                                                                                               |
| FR-1.7  | The CLI shall return deterministic exit codes for success, validation failure, cancellation, and runtime failure. (Currently uses `parseResult.InvokeAsync()` return value; explicit exit-code enum mapping deferred until after current work is approved.) |
| FR-1.8  | The CLI command surface and output contract shall be versioned/documented to reduce automation breakage risk.                                                                                                                                               |
| FR-1.9  | The studio solution structure shall support multiple frontends with dedicated WPF and CLI frontend folders/projects plus shared frontend-agnostic projects.                                                                                                 |
| FR-1.10 | The CLI host should support an entry-point class pattern (for example, `App : ConsoleAppBase`) so application behavior can be composed outside `Program.Main`.                                                                                              |
| FR-1.11 | The CLI shall support a clearly defined dependency-project configuration file argument and, where appropriate, documented aliases or defaults so the entry point remains predictable for scripts.                                                           |
| FR-1.12 | The CLI command names and argument conventions shall be documented and treated as stable automation contracts once released.                                                                                                                                |
| FR-1.13 | The CLI shall remain file/argument driven for core operation and shall not require a separate settings file to run.                                                                                                                                         |
| FR-1.14 | The CLI implementation shall use `System.CommandLine` version `2.0.8` for parsing, binding, and command composition. Documentation references should include the official repository and Microsoft docs (see References below).                             |

### FR-2: Dependency Project Contract Compatibility

| ID     | Requirement                                                                                                                                         |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-2.1 | The CLI shall load dependency-project files produced by WPF without frontend-specific adaptation code.                                              |
| FR-2.2 | Dependency-project schema handling (versioning, migration, defaults, unknown fields) shall be provided by shared code consumed by both CLI and WPF. |
| FR-2.3 | Validation outcomes for the same file shall be consistent across CLI and WPF for shared rules.                                                      |
| FR-2.4 | If schema migration is needed, the migration behavior shall be shared and deterministic.                                                            |

### FR-3: Generator Integration

| ID     | Requirement                                                                                                                                                                                                                          |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| FR-3.1 | The CLI shall execute generation through shared application services, not direct frontend-specific orchestration.                                                                                                                    |
| FR-3.2 | The CLI shall support optional pre-generation command execution using the dependency-project settings.                                                                                                                               |
| FR-3.3 | The CLI shall apply configured continue-on-failure behavior for pre-generation command outcomes.                                                                                                                                     |
| FR-3.4 | End-to-end cancellation shall propagate through CLI host, shared orchestration, and `SlnDependencyDiagramGenerator`.                                                                                                                 |
| FR-3.5 | Cancellation support in `SlnDependencyDiagramGenerator`, including automated tests, remains a prerequisite for full CLI implementation.                                                                                              |
| FR-3.6 | If CLI pre-validation requires new public generator interfaces, shared service contracts, or internal refactoring, that requirement shall be raised explicitly and implemented properly rather than worked around with ad-hoc hacks. |

### FR-4: Tool Detection and Resolution

| ID     | Requirement                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-4.1 | The CLI shall use layered tool resolution: explicit override path first, then PATH discovery via `AllOverIt.Process`, then other supported mechanisms.                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| FR-4.2 | Tool resolution shall be cross-platform (for example `where` on Windows and `which` on non-Windows where appropriate).                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| FR-4.3 | The CLI shall report unresolved required tools clearly and fail with a deterministic non-zero exit code when generation cannot proceed.                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| FR-4.4 | The CLI shall emit resolved tool path diagnostics on request.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             |
| FR-4.5 | Tool-specific handling shall follow the Single Responsibility Principle: the CLI must not perform ad-hoc checks for specific renderers (for example `d2` or `mermaid`). Implement pluggable handler objects (for example `IDiagramExporter` / `IToolResolver`) that encapsulate discovery, invocation, and diagnostics. These handlers shall be created via DI and may be surfaced by a factory configured with a dictionary of supported diagram types; the factory can know the supported types (D2, Mermaid, etc.) but must not embed implementation details for invoking those tools. |

### FR-5: Output, Logging, and Exit Behavior

| ID     | Requirement                                                                                                                                       |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-5.1 | The CLI shall stream generation and pre-generation output in real time.                                                                           |
| FR-5.2 | The CLI shall separate standard output and error output channels.                                                                                 |
| FR-5.3 | The CLI shall produce concise human-readable output by default and shall support logging for diagnostics and troubleshooting.                     |
| FR-5.4 | The CLI shall surface cancellation and failure reasons clearly.                                                                                   |
| FR-5.5 | Exit code mapping shall be stable and documented.                                                                                                 |
| FR-5.6 | The diagnostics path shall be generator and application logging, with a null logger fallback when no logger is supplied.                          |
| FR-5.7 | The CLI shall expose logging categories and verbosity levels sufficient for troubleshooting and operational diagnostics.                          |
| FR-5.8 | The CLI shall write rolling log files to a `logs` subfolder relative to the config file being processed, named `{configFileBaseName}-{Date}.txt`. |

### FR-6: Multi-Frontend Delivery and Shared Ownership

| ID     | Requirement                                                                                                                                                                                                                       |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-6.1 | WPF and CLI shall ship as first-class deliverables in the same release train, even if internal implementation order differs.                                                                                                      |
| FR-6.2 | Shared contracts and shared orchestration services shall be owned as common assets and not duplicated in frontend-specific projects.                                                                                              |
| FR-6.3 | Frontend-specific behavior shall remain in frontend projects; shared and application layers shall be frontend-agnostic.                                                                                                           |
| FR-6.4 | This CLI PRD and `PRDs/v4/Studio/SlnDependencyStudio PRD - WPF.md` shall cross-reference `PRDs/v4/Studio/SlnDependencyStudio PRD - Shared Contracts.md` and remain aligned for shared contract ownership and parity expectations. |
| FR-6.5 | If sequencing risk appears, CLI or headless shared pipeline work may be implemented before remaining WPF feature work to protect parity.                                                                                          |
| FR-6.6 | The CLI project shall create and maintain a documentation-evidence file that captures user-guide-relevant implementation notes, with class, method, and file references where useful for accurate user instruction.               |
| FR-6.7 | Documentation-evidence content shall be updated whenever PRD requirements change so guidance inputs remain synchronized with approved behavior.                                                                                   |
| FR-6.8 | During active implementation, PRD and checklist maintenance shall remain the primary documentation focus; user-guide drafting shall be a secondary focus near release hardening.                                                  |

### FR-7: Dependency Mode and Build Validation

| ID     | Requirement                                                                                                           |
| ------ | --------------------------------------------------------------------------------------------------------------------- |
| FR-7.1 | Build configuration shall use a `ProjectReference` to `SlnDependencyDiagramGenerator` for both CLI and WPF frontends. |

### FR-8: Release Automation

| ID     | Requirement                                                                                                                                             |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-8.1 | Checked-in PowerShell scripts shall drive release builds predictably for CLI artifacts.                                                                 |
| FR-8.2 | Release scripts shall validate project-reference builds and fail on parity issues between frontends.                                                    |
| FR-8.3 | Release scripts shall perform parity checks relevant to shared contracts before producing tag-ready outputs.                                            |
| FR-8.4 | Release scripts shall avoid hidden machine-local assumptions wherever practical.                                                                        |
| FR-8.5 | Release and automation guidance shall document the trust boundaries and security expectations for external pre-generation commands, including CI usage. |

---

## 6. User Experience Requirements (CLI)

### UX-1: Command Ergonomics

1. Commands should be concise and composable for script usage.
2. Help text should include practical examples for common workflows.
3. Error messages should include actionable next steps.

### UX-2: Output Clarity

1. Default output should be readable in terminals.
2. Diagnostics mode should provide additional detail for troubleshooting.
3. Cancellation and validation failures should be clearly distinguishable.

### UX-3: Automation-Friendliness

1. Exit code behavior should be stable and deterministic.
2. Output format should remain compatible across patch updates unless explicitly versioned.
3. Commands should avoid interactive prompts in non-interactive runs.

---

## 7. Technical Architecture

### 7.1 Composition Model

1. Use .NET hosting and `IServiceCollection` composition in CLI startup.
2. Keep command parsing and frontend-specific concerns in CLI project only.
3. Reuse shared orchestration services consumed by WPF.
4. Prefer `AllOverIt.GenericHost` for hosted console composition where it reduces boilerplate, especially `GenericHost.CreateConsoleHostBuilder<TConsoleApp>()` with an `IConsoleApp`/`ConsoleAppBase` implementation.

The `AllOverIt.GenericHost` demo at `Demos/AllOverIt.GenericHost/HostedConsoleAppDemo` shows this pattern with `Program.cs` hosting `App : ConsoleAppBase`, which aligns with the desired "move entry point behavior to a class" approach.

### 7.2 Shared Contracts and Boundaries

1. Dependency-project schema and migration logic live in shared code.
2. Generation request/response contracts and cancellation behavior live in shared/application layers.
3. CLI project must not reference WPF assemblies.
4. WPF-specific interaction rules must not leak into shared behavior.
5. Solution/project layout keeps frontend concerns isolated while making shared projects consumable by both frontends.

### 7.3 DI Registration Convention

The same DI conventions as WPF apply:

1. Group service registration by feature in extension methods.
2. Use `AllOverIt.DependencyInjection` auto-registration where appropriate.
3. Support transient/scoped/singleton marker patterns and filtering rules where needed.
4. Keep registration centralized and deterministic.

### 7.4 Validation Strategy

1. Shared validation rules should remain in shared/application code.
2. `FluentValidation` may be used for declarative rules.
3. `AllOverIt.Validation` helpers may be used where invoker- or context-based validation is useful.
4. `AllOverIt.Validation.Options` can be used where startup options validation is valuable.

### 7.5 Logging and Diagnostics Strategy

1. Serilog is the primary structured logging backend for CLI, file, and (future) WPF sinks.
2. The generator and renderers use `ILogger<T>` (not `IColorConsoleLogger`) so all output flows through the Serilog pipeline.
3. The CLI shall provide colorised console output via Serilog's `AnsiConsoleTheme` (or equivalent theme) on the console sink, applying color by log level (Error=red, Warning=yellow, Information=white, Debug=gray).
4. CLI output must still provide deterministic stdout/stderr behavior regardless of logging backend.
5. All generator output (project discovery, dependency listings, export progress) shall appear in both the console and the rolling file log.
6. For parity investigations, shared orchestration events should be diagnosable in both WPF and CLI logs.

### 7.6 Cross-Frontend Guardrails (CLI + WPF)

Shared requirements are documented in `PRDs/v4/Studio/SlnDependencyStudio PRD - Shared Contracts.md`. If any overlap in this CLI PRD conflicts with the shared-contracts PRD or WPF PRD, implementation must pause and the conflict must be raised to the product owner for explicit alignment before proceeding.

1. Shared contracts first: finalize shared schema and orchestration contracts before frontend divergence.
2. Shared orchestration first: run-path semantics should be shared and frontend-agnostic.
3. Dependency-mode discipline: use `ProjectReference` to `SlnDependencyDiagramGenerator` for both frontends.
4. Deterministic release automation: scripts must enforce mode, run checks, and fail fast.
5. Sequencing safety: implementation order may prioritize whichever frontend best protects shared parity at any given stage.

---

## 8. External Tooling Strategy

Use the same tool-detection conventions as WPF and the core library:

1. Explicit override paths first.
2. PATH discovery using `AllOverIt.Process` and platform-appropriate lookup.
3. User-facing diagnostics for missing tools.
4. Deterministic failure when required tools are unavailable.

---

## 9. Recommended NuGet Packages

### Required or Strongly Recommended

| Package                         | Purpose                          | Notes                                                                                                      |
| ------------------------------- | -------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `Microsoft.Extensions.Hosting`  | Startup/composition              | Aligns with WPF and shared services.                                                                       |
| `AllOverIt.GenericHost`         | Hosted console app composition   | Enables `Program` thinness via `GenericHost.CreateConsoleHostBuilder<TConsoleApp>()` and `ConsoleAppBase`. |
| `AllOverIt.Process`             | Tool discovery/process execution | Aligns with generator/tool detection strategy.                                                             |
| `AllOverIt.DependencyInjection` | DI auto-registration/decorators  | Matches established registration pattern.                                                                  |
| `FluentValidation`              | Declarative validation rules     | Shared rule style across frontends.                                                                        |
| `System.CommandLine` (v2.0.8)   | Command parsing and binding      | Preferred parser for CLI command composition; use v2.0.8. See References for docs and migration guidance.  |

### Good Candidates

| Package                           | Purpose                             | Notes                                                |
| --------------------------------- | ----------------------------------- | ---------------------------------------------------- |
| `AllOverIt.Validation`            | Validation invokers/context helpers | Useful for advanced stateless validation cases.      |
| `AllOverIt.Validation.Options`    | Options validation                  | Useful for startup validation of CLI options/config. |
| `Serilog` and `AllOverIt.Serilog` | Structured logging                  | Optional depending on CLI logging mode requirements. |

---

## 10. Non-Functional Requirements

| ID     | Requirement                                                                                                                                                                                |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| NFR-1  | CLI shall remain cross-platform where external tool dependencies allow.                                                                                                                    |
| NFR-2  | Shared code consumed by WPF and CLI shall remain frontend-agnostic.                                                                                                                        |
| NFR-3  | Runtime failures shall be explicit and script-friendly.                                                                                                                                    |
| NFR-4  | Build and test pipelines shall validate project-reference builds and remain healthy for project-reference workflows.                                                                       |
| NFR-5  | Release outputs shall be reproducible via checked-in PowerShell scripts.                                                                                                                   |
| NFR-6  | CLI command and exit-code behavior shall be stable and version-aware.                                                                                                                      |
| NFR-7  | Shared contract and orchestration behavior shall remain aligned with `PRDs/v4/Studio/SlnDependencyStudio PRD - Shared Contracts.md` and `PRDs/v4/Studio/SlnDependencyStudio PRD - WPF.md`. |
| NFR-8  | Pre-generation command execution must be treated as trusted-input behavior and documented accordingly.                                                                                     |
| NFR-9  | A CLI documentation-evidence artifact for future user guides shall be maintained in sync with PRD evolution and implementation changes.                                                    |
| NFR-10 | Documentation effort prioritization shall be: PRDs and checklists first, user-guide authoring second.                                                                                      |

---

## 11. Initial Acceptance Criteria

1. CLI can validate a dependency-project file and report deterministic success/failure.
2. CLI can run generation from a dependency-project file created by WPF.
3. Shared validation and schema behavior are consistent for the same input across WPF and CLI.
4. End-to-end cancellation works through CLI host and generator pipeline.
5. Tool detection behavior follows layered resolution and reports missing tools clearly.
6. CLI emits real-time run output with clear stdout/stderr separation.
7. Exit codes are deterministic and documented.
8. Build automation supports project-reference-based builds.
9. PowerShell release scripts produce predictable, tag-ready artifacts.
10. CLI architecture remains compatible with established packaging and distribution practices.
11. This CLI PRD and `PRDs/v4/Studio/SlnDependencyStudio PRD - WPF.md` remain aligned with `PRDs/v4/Studio/SlnDependencyStudio PRD - Shared Contracts.md` on shared contract ownership and parity rules.
12. A CLI documentation-evidence file exists and is updated alongside PRD changes with enough implementation detail to support accurate user-guide generation.
13. Documentation prioritization is observable: PRD/checklist updates are maintained during development, while user-guide drafting is deferred to end-phase hardening.
14. Any pre-validation requirement that exposes a missing generator interface or shared contract is surfaced as an explicit requirement and not solved with a temporary hack.

---

## 12. Recommended Next Step

Create a focused implementation plan for CLI Milestone 1 covering:

1. Finalize shared dependency-project contract ownership with alignment to `PRDs/v4/Studio/SlnDependencyStudio PRD - Shared Contracts.md` and `PRDs/v4/Studio/SlnDependencyStudio PRD - WPF.md`.
2. Define command surface and exit-code contract.
3. Implement shared orchestration integration and cancellation propagation.
4. Implement layered tool detection with cross-platform behavior.
5. Add deterministic PowerShell release scripts for CLI build/package flows.
6. Add parity tests that run shared golden dependency-project files through WPF and CLI paths.
7. Prepare packaging settings and release gates as needed.
8. Documentation workflow setup: create and maintain a CLI documentation-evidence file, define checklist scaffolding, and enforce PRD/checklist-first documentation cadence with user-guide drafting deferred to end-phase.

This PRD should remain a living draft while command surface, packaging policy, and shared parity tests are finalized.

---

## References

- System.CommandLine (repo): https://github.com/dotnet/command-line-api
- System.CommandLine documentation and guidance: https://learn.microsoft.com/en-us/dotnet/standard/commandline/
- Context7-compatible docs may be used where available as an alternate reference source.
