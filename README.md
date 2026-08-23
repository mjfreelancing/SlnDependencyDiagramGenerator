# SlnDependencyDiagramGenerator

Generates D2 and Mermaid dependency diagrams for Visual Studio Solutions.

![](https://img.shields.io/badge/.NET-10.0-55A9EE.svg)
![](https://img.shields.io/badge/.NET-9.0-C56EE0.svg)
![](https://img.shields.io/badge/.NET-8.0-FF8C67.svg)

[![NuGet](https://img.shields.io/nuget/vpre/SlnDependencyDiagramGenerator?color=E3505C)](https://www.nuget.org/packages/SlnDependencyDiagramGenerator/absoluteLatest/)
[![NuGet](https://img.shields.io/nuget/dt/SlnDependencyDiagramGenerator?color=FFC33C)](https://www.nuget.org/packages/SlnDependencyDiagramGenerator/absoluteLatest/)

---

**SlnDependencyDiagramGenerator** parses Visual Studio solutions (`.sln` / `.slnx`), resolves project, package, and framework dependencies, and produces dependency diagrams in D2 and/or Mermaid formats — with optional image export (PNG, SVG, PDF) via the corresponding CLI tools.

The repository ships three deliverables around a shared core:

| Deliverable                         | What it is                         | Best for                                                                |
| ----------------------------------- | ---------------------------------- | ----------------------------------------------------------------------- |
| **`SlnDependencyDiagramGenerator`** | A .NET library (NuGet package)     | Embedding diagram generation in your own code, tools, or build steps    |
| **SlnDependencyStudio CLI**         | A cross-platform command-line tool | Automating generation in scripts and CI pipelines                       |
| **SlnDependencyStudio WPF**         | A Windows desktop application      | Authoring, editing, and running dependency projects with a graphical UI |

Pre-built versions of the **SlnDependencyStudio CLI** and **WPF** application are distributed together as **SlnDependencyStudio** — download them from the [Releases page](https://github.com/mjfreelancing/SlnDependencyDiagramGenerator/releases).

This [D2 example](./Studio%20Diagrams/net10.0/d2/slndependencydiagramgenerator.png) and [Mermaid example](./Studio%20Diagrams/net10.0/mmd/slndependencydiagramgenerator.png) were produced from the solution in this repository.

---

## Table of Contents

- [Quick Start](#quick-start)
- [The Core Library (NuGet)](#the-core-library-nuget)
- [SlnDependencyStudio CLI](#slndependencystudio-cli)
- [SlnDependencyStudio WPF](#slndependencystudio-wpf)
- [Repository Structure](#repository-structure)
- [License](#license)

---

## Quick Start

### Using the core library via NuGet

```shell
dotnet add package SlnDependencyDiagramGenerator
```

See the [DiagramGeneratorSample](./Samples/DiagramGeneratorSample/) project for a complete working example that loads configuration from `appsettings.json` and runs the generator against the solution in this repository.

---

## The Core Library (NuGet)

`SlnDependencyDiagramGenerator` is a .NET library targeting `net8.0`, `net9.0`, and `net10.0`, published to [NuGet](https://www.nuget.org/packages/SlnDependencyDiagramGenerator/).

Key capabilities:

- Parses `.sln` / `.slnx` solutions and resolves project, package, framework, and transitive dependencies from restored `project.assets.json` files.
- Generates per-project (**Individual**) and solution-level (**All**) diagrams in D2 and/or Mermaid formats.
- Auto-discovers target frameworks from restored project assets — no framework list required in configuration.
- Detects multi-version package conflicts, grouping them visually and reporting them in the dependency summary.
- Supports regex include/exclude filters plus package and framework exclusions.
- Produces a `Dependency Summary.md` and can export PNG, SVG, and/or PDF images via the [D2 CLI](https://d2lang.com/tour/install/) and [Mermaid CLI (mmdc)](https://github.com/mermaid-js/mermaid-cli#installation).

**Public API:** The main entry point is `IDependencyGenerator`, registered in DI and available from the `SlnDependencyDiagramGenerator.Generator` namespace:

| Member                                                                                                   | Description                                                                                    |
| -------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `void ValidateConfiguration(DependencyGeneratorConfig configuration)`                                    | Validates a configuration, throwing `FluentValidation.ValidationException` on any violation.   |
| `Task CreateDiagramsAsync(DependencyGeneratorConfig configuration, CancellationToken cancellationToken)` | Generates summaries, diagram files, and optional images for every discovered target framework. |

Configuration is modelled by three option classes exposed on `DependencyGeneratorConfig` — `GeneratorSolutionOptions` (solution path, filters, exclusions, scopes), `GeneratorDiagramOptions` (direction, styles, grouping, formats), and `GeneratorExportOptions` (root path, clear behaviour, image formats). See [configuration.md](./Docs/configuration.md) for the complete reference.

**Dependency Injection:** Register all services with a single call:

```csharp
using SlnDependencyDiagramGenerator.Extensions;

var services = new ServiceCollection();
var (serviceCollection, validationRegistry) = services.AddSlnDependencyDiagramGenerator();
```

`AddSlnDependencyDiagramGenerator()` returns an `SlnDependencyDiagramGeneratorRegistration` containing the service collection and a validation registry, so additional registrations can be chained.

**Sample project:** [`Samples/DiagramGeneratorSample`](./Samples/DiagramGeneratorSample/) is a thin console app that binds `DependencyGeneratorConfig` from `appsettings.json` (plus optional `SETTINGS_VARIANT` overlays), resolves `IDependencyGenerator` from DI, and calls `CreateDiagramsAsync()`:

```shell
dotnet run --project Samples/DiagramGeneratorSample
```

[`Samples/NugetConflictSample`](./Samples/NugetConflictSample/) is a companion project that deliberately references different package versions than the library, so the multi-version conflict table in the dependency summary can be validated. See [Samples/README.md](./Samples/README.md) for details on the sample's configuration model.

---

## SlnDependencyStudio CLI

`SlnDependencyStudio.Cli` is a cross-platform console tool (`System.CommandLine`) that reads a saved dependency-project file (`.sds`) and runs the same generation pipeline as the WPF application — designed for automation in scripts and CI pipelines.

### Quick Start

```shell
# Publish the CLI
dotnet publish Studio\SlnDependencyStudio.Cli -o D:\tools\SlnDependencyStudio

# Validate a project file
SlnDependencyStudio.Cli validate --pf sample.sds

# Run generation
SlnDependencyStudio.Cli run --pf sample.sds
```

- **Commands:** `validate` (check a `.sds` file for configuration errors) and `run` (validate, optionally restore the solution, run optional pre/post-generation commands, and generate the diagrams).
- **Key options:** `--projectFile` / `--pf` (required) and `--verbose` / `-v` (Debug-level console logging).
- **Deterministic exit codes** (1001–1008, 1999) make scripting and CI integration predictable; rolling file logs are written to a `logs` subfolder beside the `.sds` file for troubleshooting past runs.

> **Note:** Both frontends share the `.sds` document format via `SlnDependencyStudio.Shared`, so a project authored in the WPF application runs unchanged in the CLI and vice versa.

See the [CLI User Guide](./Docs/cli.md) for installation, the full command reference, and many usage examples.

---

## SlnDependencyStudio WPF

`SlnDependencyStudio.Wpf` is a Windows desktop application (Windows 10+, `net10.0-windows10.0.19041`) built with ReactiveUI and MaterialDesignThemes. It provides a full graphical interface over the generator so you can author, edit, and run dependency projects without hand-editing JSON.

### Quick Start

Build and run `SlnDependencyStudio.Wpf` from Visual Studio or the command line:

```shell
dotnet run --project Studio\SlnDependencyStudio.Wpf
```

- **IDE-style shell** — File and Run menus, a navigation sidebar, a central configuration editor, and a docked output panel.
- **Five configuration pages** — Project, Solution, Export, Diagrams, and Pipeline, including restore and pre/post-generation command settings with live d2/mmdc tool detection.
- **Analyse / Generate** — `Shift+F5` runs a pre-flight dry run; `F5` runs the full pipeline with real-time streaming output and user-initiated cancellation.
- **Output panel** — Verbose/Wrap/Auto-scroll toggles, Cancel, Clear, Copy All, and Save As; preferences persist across sessions.
- **Settings & state** — Default project folder, explicit d2/mmdc paths, log retention, Light/Dark theme, recent projects (capped at 10), and window placement, all persisted in AppData.

See the [WPF User Guide](./Docs/wpf-user-guide.md) for a full walkthrough with screenshots.

---

## Repository Structure

```
├── Source/
│   └── SlnDependencyDiagramGenerator/    Core library (NuGet package)
├── Studio/
│   ├── SlnDependencyStudio.Shared/       Shared document, service, and process-execution contracts
│   ├── SlnDependencyStudio.Cli/          Cross-platform CLI frontend
│   └── SlnDependencyStudio.Wpf/          Windows desktop frontend
├── Samples/
│   ├── DiagramGeneratorSample/           Console sample using the library directly
│   └── NugetConflictSample/              Sample demonstrating multi-version packages
├── Tests/
│   ├── Source/                           Core library unit + integration tests
│   └── Studio/                           Shared, CLI, and WPF unit + integration tests
├── Docs/                                 User documentation
└── Output/                               Generated diagram output (when running the sample)
```

---

## License

See [LICENSE](./LICENSE).
