# SlnDependencyDiagramGenerator

Generates D2 and Mermaid dependency diagrams for Visual Studio Solutions.

![](https://img.shields.io/badge/.NET-10.0-55A9EE.svg)
![](https://img.shields.io/badge/.NET-9.0-C56EE0.svg)
![](https://img.shields.io/badge/.NET-8.0-FF8C67.svg)

[![NuGet](https://img.shields.io/nuget/vpre/SlnDependencyDiagramGenerator?color=E3505C)](https://www.nuget.org/packages/SlnDependencyDiagramGenerator/absoluteLatest/)
[![NuGet](https://img.shields.io/nuget/dt/SlnDependencyDiagramGenerator?color=FFC33C)](https://www.nuget.org/packages/SlnDependencyDiagramGenerator/absoluteLatest/)

---

**SlnDependencyDiagramGenerator** is a .NET library that parses Visual Studio solutions (`.sln` / `.slnx`), resolves project, package, and framework dependencies, and produces dependency diagrams in D2 and/or Mermaid formats. It can optionally export images (PNG, SVG, PDF) via the corresponding CLI tools.

Two front-end applications sit on top of the library:

- **SlnDependencyStudio CLI** — a cross-platform command-line tool for running dependency-project files, suitable for scripts and CI pipelines.
- **SlnDependencyStudio WPF** — a Windows desktop application for authoring, editing, and running dependency projects with a full graphical interface.

This [example](./Samples/Output/net9.0/d2/slndependencydiagramgenerator.png) was produced from the solution in this repository.

---

## Quick Start

### Using the core library via NuGet

```shell
dotnet add package SlnDependencyDiagramGenerator
```

See the [DiagramGeneratorSample](./Samples/DiagramGeneratorSample/) project for a complete working example that loads configuration from `appsettings.json` and runs the generator against the solution in this repository.

### Using the CLI tool

```shell
# Publish the CLI
dotnet publish Studio\SlnDependencyStudio.Cli -o D:\tools\SlnDependencyStudio

# Validate a configuration file
SlnDependencyStudio.Cli validate --cf sample.sds

# Run generation
SlnDependencyStudio.Cli run --cf sample.sds
```

### Using the WPF application

Build and run `SlnDependencyStudio.Wpf` from Visual Studio or the command line:

```shell
dotnet run --project Studio\SlnDependencyStudio.Wpf
```

---

## Documentation

| Document                                           | Description                                       |
| -------------------------------------------------- | ------------------------------------------------- |
| [Docs/README.md](./Docs/README.md)                 | Index of all documentation                        |
| [Docs/configuration.md](./Docs/configuration.md)   | Complete reference for every configuration option |
| [Docs/cli.md](./Docs/cli.md)                       | SlnDependencyStudio CLI user guide                |
| [Docs/wpf-user-guide.md](./Docs/wpf-user-guide.md) | SlnDependencyStudio WPF user guide                |

---

## Repository Structure

```
├── Source/
│   └── SlnDependencyDiagramGenerator/    Core library (NuGet package)
├── Studio/
│   ├── SlnDependencyStudio.Cli/           Cross-platform CLI frontend
│   ├── SlnDependencyStudio.Shared/        Shared contracts and services
│   └── SlnDependencyStudio.Wpf/           Windows desktop frontend
├── Samples/
│   ├── DiagramGeneratorSample/            Console sample using the library directly
│   └── NugetConflictSample/               Sample demonstrating multi-version packages
├── Tests/
├── Output/                                Generated diagram output folder
└── Docs/                                  Documentation
---

## License

See [LICENSE](./LICENSE).
  - [Project README (features and API entry points)](https://github.com/microsoft/vs-solutionpersistence/blob/main/README.md)
```
