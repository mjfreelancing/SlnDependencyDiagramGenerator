# Product Requirements Document — SlnDependencyStudio

**Date:** May 2026  
**Status:** Draft  
**Scope:** WPF desktop application for authoring, running, and managing dependency-diagram projects backed by `SlnDependencyDiagramGenerator`, with shared contracts aligned to `PRDs/v4/SlnDependencyStudio PRD - Shared Contracts.md` and `PRDs/v4/SlnDependencyStudio PRD - CLI.md`

---

## 1. Executive Summary

`SlnDependencyStudio` is a desktop application for creating, editing, and running saved dependency-diagram projects against `.sln` and `.slnx` solutions. It provides a GUI over the existing `SlnDependencyDiagramGenerator` library so a user can configure solution inputs, diagram options, export settings, and generation workflows without hand-editing JSON files or running console commands manually.

The application must be structured so it can absorb future capabilities from `SlnDependencyDiagramGenerator` without repeated architectural churn. The first version focuses on project authoring, validation, CLI tool detection, generation orchestration, and transparent display of generation output.

The first release is explicitly Windows 10-only, targeting `net10.0-windows10.0.19041`, and will use `MahApps.Metro` together with the Material Design bridge package for theming and control styling.

---

## 2. Product Goals

### Goals

1. Provide a WPF desktop application for configuring and running dependency generation jobs.
2. Use dependency injection and ReactiveUI throughout the application, with ReactiveUI used as the MVVM foundation for view-model state, commands, menu enablement, validation, and other UI behavior.
3. Reuse `SlnDependencyDiagramGenerator` through shared contracts and conditional dependency mode support: local project references for development and package references for release validation.
4. Support a first-class saved document concept called a **dependency project**.
5. Make complex configuration approachable without hiding important options from advanced users.
6. Detect `d2` and Mermaid CLI availability and expose related options only when those tools are available. When not available, the option should still be shown but not actionable, or otherwise made obvious. This will cater for visibility of future formats.
7. Surface all generator and CLI output in real time while generation is running. If the generator requires callbacks or events for progress / discovery notifications then this should be considered and can be added while the application is under development. The user must confirm all extension considerations.
8. Keep the architecture extensible so future features can be added without rewriting the shell.
9. Standardise the UI stack on `MahApps.Metro` with the `MaterialDesignThemes.MahApps` bridge for a professional, consistent Windows desktop experience.
10. Support an optional pre-generation command step (for example BAT, PS1, or EXE) so users can run prerequisite actions such as rebuilds before diagram generation.
11. Treat WPF and CLI as first-class delivery targets in the same release, with shared document and orchestration contracts.
12. Keep WPF Windows-only while preserving a cross-platform-compatible shared core so CLI can run on non-Windows environments.
13. Support dual dependency modes for local and release builds so projects can use local project references during development and package references for release validation.
14. Provide predictable PowerShell-based release scripts so tagged releases are reproducible and low-risk.

### Non-Goals

1. Replacing the underlying diagram-generation engine.
2. Providing an in-app live diagram preview in the first draft.
3. Monetisation, licensing workflows, or marketplace distribution concerns.
4. Cross-platform desktop support in the first release. This is a Windows 10-only WPF application.

---

## 3. Target Users

The primary user is a .NET developer working with medium-to-large solutions who wants to inspect package and framework dependencies, generate diagrams repeatedly, compare configuration outcomes, and keep output artifacts organised. The user is technically capable, comfortable with IDE-style tools, and values keyboard efficiency, transparency, and predictable workflows over overly simplified UX.

---

## 4. Product Overview

`SlnDependencyStudio` manages two related concerns:

1. **Dependency projects**: saved documents that define what solution to process and how diagrams should be generated.
2. **Application settings**: user-scoped preferences that control application behaviour, such as default folders and recent files.

The application hosts the `SlnDependencyDiagramGenerator` library directly and acts as an orchestration and authoring layer above it. It must support both creating a project from defaults and creating one by importing an existing dependency project file as a starting point.

---

## 5. Functional Requirements

### FR-1: Application Shell

| ID     | Requirement                                                                                                                                                                                                                |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-1.1 | The application shall be a WPF desktop application located at the repository root in its own project.                                                                                                                      |
| FR-1.2 | The application shall use `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.DependencyInjection` for composition and application startup.                                                                           |
| FR-1.3 | The application shall use ReactiveUI as the MVVM foundation for presentation logic, command handling, property change notification, validation-friendly state management, and UI enablement such as button and menu state. |
| FR-1.4 | The application shall be structured to support future expansion without changing the saved dependency project file format unnecessarily.                                                                                   |
| FR-1.5 | The first release shall target Windows 10 only.                                                                                                                                                                            |
| FR-1.6 | The application shall target `net10.0-windows10.0.19041`.                                                                                                                                                                  |
| FR-1.7 | The solution structure shall reserve room for multiple frontends by placing the WPF application under a dedicated `Wpf` sub-folder within the studio solution area.                                                        |
| FR-1.8 | A shared project shall exist for dependency-project contracts and orchestration-facing models consumed by both WPF and CLI frontends.                                                                                      |

### FR-2: Dependency Project Lifecycle

| ID     | Requirement                                                                                                                                 |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-2.1 | The user shall be able to create a new dependency project from application defaults.                                                        |
| FR-2.2 | The user shall be able to create a new dependency project by loading an existing dependency project file and using it as the initial state. |
| FR-2.3 | The user shall be able to open, edit, save, and save-as a dependency project file.                                                          |
| FR-2.4 | The application shall track unsaved changes and prompt the user before discarding them.                                                     |
| FR-2.5 | The application shall maintain a recent dependency project list.                                                                            |
| FR-2.6 | The application shall allow a dependency project to store metadata including at least `ProjectName` and `Description`.                      |
| FR-2.7 | The dependency project schema shall use an extensible envelope so future metadata can be added without breaking existing files.             |

### FR-3: Dependency Project File Format

The dependency project file shall not serialize `DependencyGeneratorConfig` alone. It shall be wrapped in an extensible document model.

Proposed shape:

```json
{
  "schemaVersion": 1,
  "metadata": {
    "projectName": "My Solution Audit",
    "description": "Tracks the default diagram settings for the main repo"
  },
  "generatorConfig": {
    "projects": {},
    "diagram": {},
    "export": {}
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

Requirements for this format:

| ID     | Requirement                                                                                                                                                            |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-3.1 | The file format shall include a schema version.                                                                                                                        |
| FR-3.2 | The file format shall contain `metadata` with `projectName` and `description`.                                                                                         |
| FR-3.3 | The file format shall contain a `generatorConfig` payload compatible with the current `DependencyGeneratorConfig` model.                                               |
| FR-3.4 | Unknown future fields shall be ignored when loading, where practical.                                                                                                  |
| FR-3.5 | The file format shall support optional pre-generation command configuration including enabled state, command path, arguments, working directory, and failure behavior. |

### FR-4: Configuration Editing

| ID     | Requirement                                                                                                                                                                                                                                                  |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| FR-4.1 | The user shall be able to edit all supported `DependencyGeneratorConfig` properties through the UI.                                                                                                                                                          |
| FR-4.2 | The UI shall support solution path selection, include/exclude patterns, package/framework exclusions, per-scope enablement, transitive depth, grouping options, styling, output path, clear-contents behaviour, diagram formats, and image formats.          |
| FR-4.3 | Configuration sections shall be organised into a clean, user-approved grouping of common and advanced settings so the UI remains approachable without hiding important capabilities. The exact show/hide boundaries shall be approved before implementation. |
| FR-4.4 | Validation errors shall be presented inline and before generation starts, using ReactiveUI.Validation for validation binding and UI control state presentation wherever practical.                                                                           |
| FR-4.5 | The UI shall support browsing for solution files, dependency project files, and export folders.                                                                                                                                                              |
| FR-4.6 | The UI shall support starting from defaults or cloning from an existing dependency project as a faster authoring path.                                                                                                                                       |

### FR-5: Application Settings

| ID     | Requirement                                                                                                               |
| ------ | ------------------------------------------------------------------------------------------------------------------------- |
| FR-5.1 | The application shall store user-scoped settings in a settings file rather than the Windows registry.                     |
| FR-5.2 | Settings shall include a default location for loading and saving dependency project files.                                |
| FR-5.3 | Settings shall include a browse workflow for selecting the default dependency project folder.                             |
| FR-5.4 | Settings should also support recent files and window-state persistence if that is adopted in the implementation.          |
| FR-5.5 | Settings shall support explicit overrides for required external tool locations when PATH-based discovery is insufficient. |
| FR-5.6 | The settings mechanism shall be chosen so additional user preferences can be added without redesigning persistence.       |

### FR-6: Generator Integration

| ID      | Requirement                                                                                                                                                                                                 |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-6.1  | The application shall reference `SlnDependencyDiagramGenerator` through a conditional dependency mode that supports project references for local development and package references for release validation. |
| FR-6.2  | The application shall validate configuration before invoking generation.                                                                                                                                    |
| FR-6.3  | The application shall execute generation through an application service layer rather than directly from the view.                                                                                           |
| FR-6.4  | The application shall present success, warning, and failure outcomes clearly after generation completes.                                                                                                    |
| FR-6.5  | The application shall provide an action to open Windows File Explorer at the export root for the most recent run.                                                                                           |
| FR-6.6  | The application shall make clear that generated `.d2` and `.mmd` files are stored in renderer-specific subfolders under the target framework output folder.                                                 |
| FR-6.7  | The generation workflow shall support an optional pre-generation command that executes before diagram generation starts.                                                                                    |
| FR-6.8  | The pre-generation command shall support at least executable files and common script entry points used in Windows workflows, including `.bat` and `.ps1`.                                                   |
| FR-6.9  | If configured, pre-generation command success or failure shall be evaluated before diagram generation, with behavior controlled by a per-project continue-on-failure option.                                |
| FR-6.10 | The generation workflow shall support user-initiated cancellation end-to-end, including propagation of cancellation through the application service layer and into `SlnDependencyDiagramGenerator`.         |
| FR-6.11 | Full cancellation support in `SlnDependencyDiagramGenerator`, including automated tests, shall be implemented before SlnDependencyStudio application implementation begins.                                 |

### FR-7: CLI Tool Detection and Gating

| ID     | Requirement                                                                                                                                                                                                                      |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-7.1 | The application shall detect whether required external tools are available before enabling dependent options.                                                                                                                    |
| FR-7.2 | Detection shall use a layered resolution strategy that checks explicit tool-path overrides first, then process-locating via `where` or `which` using `AllOverIt.Process`, and then any additional supported discovery mechanism. |
| FR-7.3 | If `d2` is not available, D2 image-export options that depend on the CLI shall be disabled and explained.                                                                                                                        |
| FR-7.4 | If Mermaid image export requires `mmdc` and it is not available, Mermaid image-export options shall be disabled and explained.                                                                                                   |
| FR-7.5 | The application shall provide a visible tool-status view that shows whether each CLI was found and, where possible, the resolved executable path or version.                                                                     |
| FR-7.6 | The user shall be able to manually re-scan for CLI tools without restarting the application.                                                                                                                                     |
| FR-7.7 | The user shall be able to browse for and save explicit executable paths for required tools when those tools are not discoverable through PATH.                                                                                   |

### FR-8: Generation Output Experience

| ID     | Requirement                                                                                                                                                                              |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-8.1 | The application shall display all output generated during the run pipeline, including output from pre-generation commands and CLI output from `d2` and `mmdc`.                           |
| FR-8.2 | Output shall stream in real time while generation is in progress.                                                                                                                        |
| FR-8.3 | Standard output and error output should be visually distinguishable.                                                                                                                     |
| FR-8.4 | The user shall be able to review the complete output from the most recent run without leaving the application.                                                                           |
| FR-8.5 | The generation view shall expose the most recent export root and provide an action to open it in Windows File Explorer.                                                                  |
| FR-8.6 | The first release shall not require an in-app diagram preview.                                                                                                                           |
| FR-8.7 | While generation is in progress, ReactiveUI command/state binding shall disable configuration editing and generation-adjacent UI controls, while keeping cancel-related actions enabled. |
| FR-8.8 | While generation is in progress, the application shall prevent the main window from closing.                                                                                             |

### FR-9: Navigation and Productivity

| ID     | Requirement                                                                                                                                   |
| ------ | --------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-9.1 | The shell should follow an IDE-style layout suitable for technical users.                                                                     |
| FR-9.2 | The application should provide keyboard shortcuts for common operations including new, open, save, save-as, generate, and open output folder. |
| FR-9.3 | The application should surface an obvious empty state when no dependency project is loaded.                                                   |
| FR-9.4 | The application should present recent projects prominently.                                                                                   |
| FR-9.5 | The application should expose validation status per major configuration section.                                                              |

### FR-10: Candidate Value-Add Features

The following features are valuable and should be considered as planned-but-not-committed scope unless explicitly promoted into the initial implementation:

1. NuGet conflict detector panel.
2. Solution statistics dashboard.
3. Search and navigate panel for projects/packages.
4. Export presets for commonly used output combinations.
5. Inline regex tester against the loaded solution.
6. Open project in Visual Studio or VS Code from a selected result or node-driven context.
7. Snapshot history or diff support for dependency projects and outputs.

### FR-11: Multi-Frontend Delivery and Build Modes

| ID      | Requirement                                                                                                                                                                                                                      |
| ------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-11.1 | The WPF and CLI deliverables shall be treated as first-class artifacts for the same release train, even if implementation sequencing differs.                                                                                    |
| FR-11.2 | The dependency project document contract (schema, serialization, defaults, and migration behavior) shall be implemented in shared code consumed by both frontends.                                                               |
| FR-11.3 | The WPF implementation shall not introduce frontend-specific behavior into shared orchestration code that would prevent CLI parity.                                                                                              |
| FR-11.4 | Build configuration shall support a conditional dependency mode switch (for example, `UseLocalGeneratorProjectRefs`) so local builds can use `ProjectReference` and release builds can use `PackageReference` for the generator. |
| FR-11.5 | CI and release validation shall execute in both dependency modes to detect project-reference vs package-reference drift before tagging.                                                                                          |
| FR-11.6 | PowerShell release scripts shall produce predictable release outputs and enforce the selected dependency mode explicitly.                                                                                                        |
| FR-11.7 | This WPF PRD and `PRDs/v4/SlnDependencyStudio PRD - CLI.md` shall cross-reference `PRDs/v4/SlnDependencyStudio PRD - Shared Contracts.md` so document-format and pipeline behavior remain aligned across both frontends.         |

---

## 6. User Experience Requirements

### UX-1: Shell Layout

Recommended default layout for the draft:

1. Left navigation rail or sidebar for recent projects, project actions, and top-level sections.
2. Centre workspace for the active dependency project editor.
3. Bottom docked output panel for generation logs and CLI output.

This layout matches developer expectations shaped by IDE tools and gives generation output a persistent home without obscuring the configuration surface.

### UX-2: Progressive Disclosure

The editor should separate common settings from advanced ones. The initial experience should emphasise:

1. Solution path.
2. Dependency project name and description.
3. Output folder.
4. Diagram formats.
5. High-level scope toggles.

Advanced areas such as regex filters, grouping nuance, styles, and transitive-depth tuning should remain available through dedicated sections rather than one long scrolling form.

### UX-3: Validation and Feedback

1. Field-level validation should appear inline.
2. Pre-generation validation should prevent avoidable failures.
3. Long-running generation actions should show clear activity state, disable non-cancel UI actions through ReactiveUI state binding, keep cancel actions enabled, and prevent closing the app while generation is in progress.
4. Post-run feedback should include success/failure, elapsed time, and quick access to the export root.

### UX-4: First-Run Experience

The empty state should clearly offer:

1. Create from defaults.
2. Create from existing dependency project.
3. Open recent dependency project.
4. Go to application settings.

This is important because the application introduces the new concept of a dependency project and should explain it immediately.

---

## 7. Technical Architecture

### 7.1 Composition Model

The application should use the .NET hosting model:

1. `HostBuilder` or `Host.CreateDefaultBuilder()` for startup.
2. `IServiceCollection` for DI registrations.
3. ReactiveUI configured during application startup.
4. Application services separated from view models.

### 7.2 ReactiveUI Strategy

The application should use ReactiveUI for:

1. Reactive view models.
2. Command orchestration.
3. Activation-aware view models.
4. Validation state binding.
5. Observable streaming of generation output.

The UI layer should remain thin. Page, window, and form code-behind should be minimal and limited to view wiring, lifecycle hookup, and platform-specific concerns. Business logic should live in feature-oriented services within the same application project, following a vertical-slice style organization so each feature owns its own UI, state, and service surface. ReactiveUI.Validation should be the primary mechanism for validation binding, command enablement, and UI control state presentation.

### 7.3 AllOverIt.ReactiveUI / WPF Considerations

The existing `AllOverIt.ReactiveUI` and `AllOverIt.ReactiveUI.Wpf` projects demonstrate useful patterns already aligned with the requested stack:

1. `ActivatableViewModel` provides an activation-friendly base class.
2. `ViewFactory` and WPF registration extensions such as `RegisterWindowTransient<TViewModel, TView>()` and `RegisterUserControlTransient<TViewModel, TView>()` support DI-backed view creation.
3. `ReactiveWindowViewHandler` and `ViewRegistry` patterns provide a path for window/dialog orchestration if the application grows beyond a single shell window.
4. The `ViewRegistryDemo` shows a concrete ReactiveUI v23 startup sequence using `RxAppBuilder.CreateReactiveUIBuilder()`, followed by `.WithCoreServices()`, `.WithWpf()`, and `.BuildApp()`, which is worth following when the studio bootstraps ReactiveUI.

These packages should be considered supportive infrastructure rather than mandatory abstraction for every feature. The application should use them where they reduce boilerplate and match the final navigation model.

### 7.4 Service Boundaries

Recommended service split:

1. `IDependencyProjectService` for open/save/new operations.
2. `IApplicationSettingsService` for user settings persistence.
3. `IToolDetectionService` for `d2` and Mermaid CLI discovery.
4. `IGenerationService` for running the generator and streaming output.
5. `IExplorerService` for opening Explorer at the export root.
6. `IRecentProjectsService` for MRU management.

### 7.5 Model Layer

Recommended document model:

1. `DependencyProjectDocument`
2. `DependencyProjectMetadata`
3. `DependencyGeneratorConfig`
4. Optional future extension payloads

This keeps the generator configuration isolated but still allows the studio to evolve independently.

### 7.6 Library Research Rule

Any third-party library used by the application shall be checked against Context7 documentation before implementation choices are made, so the app can use the latest supported features and avoid reinventing capabilities that already exist in the library.

### 7.7 Logging Strategy

The application should use Serilog as the primary application logger so logs can be captured both on disk and in the UI. The recommended approach is a rolling file sink for durable review, plus an in-memory or observable sink for live presentation inside the app. The existing `AllOverIt.Serilog` helpers are a good fit for the UI side of this requirement, especially the observable sink and circular-buffer sink patterns.

### 7.8 Automated UI Testing

The application should be designed for automated end-to-end UI testing using stable UI Automation identifiers on key controls. The preferred external automation tool is FlaUI. UI automation coverage should focus first on navigation, control state changes, and validation notifications during configuration editing; lower-level behavior should continue to be tested through ViewModel and service-level tests.

### 7.9 DI Registration Convention

`AllOverIt.DependencyInjection` provides the auto-registration mechanism used throughout the codebase. The pattern below is taken directly from its public demo (`AllOverIt.DependencyInjection/Demos/AutoRegistrationDemo` and `ExternalDependencies`) and **must be followed in SlnDependencyStudio**.

#### Marker interfaces for lifetime

Three empty marker interfaces act as lifetime anchors. Any class that should be auto-registered implements the appropriate one:

```csharp
// IStudioTransientDependency.cs
public interface IStudioTransientDependency;

// IStudioScopedDependency.cs
public interface IStudioScopedDependency;

// IStudioSingletonDependency.cs
public interface IStudioSingletonDependency;
```

`AllOverIt.DependencyInjection` scans an assembly for all concrete classes that implement the requested service type and registers them against that type automatically.

#### Assembly anchor (DependencyRegistrar)

Each participating project declares one minimal sealed class that inherits `ServiceRegistrarBase` (from `AllOverIt.DependencyInjection`). It contains no members — its only purpose is to give the scanner a type by which it can locate the correct assembly:

```csharp
// DependencyRegistrar.cs  (mirrors ExternalRegistrar in the AllOverIt demo)
internal sealed class DependencyRegistrar : ServiceRegistrarBase
{
}
```

This is exactly the pattern used in the `ExternalDependencies` demo project:

```csharp
// From AllOverIt.DependencyInjection demo — ExternalDependencies/ExternalRegistrar.cs
public sealed class ExternalRegistrar : ServiceRegistrarBase
{
}
```

#### Feature/module grouping via extension methods

Registration is grouped by feature or concern inside **`static` extension methods on `IServiceCollection`**. Each group lives in an `Extensions/ServiceCollectionExtensions.cs` file within the relevant feature or project folder. The startup bootstrapper then composes everything as a flat, self-documenting chain:

```csharp
host.Services
    .AddStudioDependencies()   // scoped + singleton auto-registration for the main assembly
    .AddLogging()              // Serilog + AllOverIt.Serilog sinks
    .AddDiagramGeneration()    // generator pipeline services
    .AddSettingsPersistence()  // settings load/save
    .AddToolDetection();       // CLI tool discovery
```

Each `Add…` method is responsible for its own group only and must not reach into another group's concerns. Every method returns `IServiceCollection` so calls can be chained fluently.

#### Auto-registration call shape

Three generic overloads are available from `AllOverIt.DependencyInjection.Extensions` — `AutoRegisterTransient`, `AutoRegisterScoped`, and `AutoRegisterSingleton` — all sharing the same signature shape. A filter lambda is supplied to exclude the marker interface itself from being registered as a service type:

```csharp
public static IServiceCollection AddStudioDependencies(this IServiceCollection services)
{
    services.AutoRegisterTransient<DependencyRegistrar, IStudioTransientDependency>(config =>
    {
        config.Filter((serviceType, implementationType) => serviceType != typeof(IStudioTransientDependency));
    });

    services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(config =>
    {
        config.Filter((serviceType, implementationType) => serviceType != typeof(IStudioScopedDependency));
    });

    services.AutoRegisterSingleton<DependencyRegistrar, IStudioSingletonDependency>(config =>
    {
        config.Filter((serviceType, implementationType) => serviceType != typeof(IStudioSingletonDependency));
    });

    return services;
}
```

This mirrors the call in the `AutoRegistrationDemo` entry point:

```csharp
// From AllOverIt.DependencyInjection demo — AutoRegistrationDemo/Program.cs
services
    .AutoRegisterSingleton<ExternalRegistrar, IRepository>()
    .Decorate<IRepository, DecoratedRepository>();
```

#### Filtering additional interfaces (ISP)

When a class implements multiple interfaces — as is common when Interface Segregation Principle is applied — the scanner will attempt to register the implementation against _every_ interface it finds, including ones that should never be resolved through this mechanism (for example, generic validation interfaces). The filter lambda can handle this by checking for generic type definitions and explicitly excluding those that are not wanted:

```csharp
services.AutoRegisterScoped<DependencyRegistrar, IStudioScopedDependency>(config =>
{
    config.Filter((serviceType, implementationType) =>
    {
        if (serviceType.IsGenericType)
        {
            var genericTypeDefinition = serviceType.GetGenericTypeDefinition();

            // Only filter out the generic interfaces we know should not be auto-registered
            return !(genericTypeDefinition == typeof(IValidator<>) || genericTypeDefinition == typeof(ValidatorBase<>));
        }

        return serviceType != typeof(IStudioScopedDependency);
    });
});
```

The pattern is: return `true` to keep the registration, `false` to suppress it. The non-generic base case always suppresses the marker interface itself; the generic case suppresses any specific open-generic interfaces that should be registered by other means (such as FluentValidation's own scanner).

#### Options / configuration binding

Configuration sections are bound using `ConfigureOptions<TSetup>()` (a setup class implementing `IConfigureOptions<T>`) and then exposed for direct injection without `IOptions<T>` wrapping via a shared `AddSingletonFromOptions<T>()` extension helper:

```csharp
services
    .ConfigureOptions<GeneratorOptionsSetup>()
    .AddSingletonFromOptions<GeneratorOptions>();
```

`AddSingletonFromOptions<T>()` is a one-liner extension method on `IServiceCollection` that resolves `IOptions<T>.Value` and registers the unwrapped instance as a singleton `T`. Defining it once and reusing it avoids repeating the same `IOptions<T>` unwrapping boilerplate at every call site.

#### Key rules

- **No registration logic outside `Add…` extension methods** — all composition happens in these methods; constructors and properties must not perform registration.
- **One `ServiceCollectionExtensions.cs` per feature folder** — this is the single place to add or remove registrations for that feature.
- **Always filter out the marker interface itself** — prevents the marker from being registered as a service type in its own right.
- **Filter generics explicitly when ISP is applied** — if a class implements multiple interfaces including generic ones that belong to a different registration mechanism, suppress those in the filter lambda as shown above.
- **`AllOverIt.DependencyInjection` NuGet package is required** — `ServiceRegistrarBase`, `AutoRegisterTransient`, `AutoRegisterScoped`, and `AutoRegisterSingleton` all come from this package.

### 7.10 Validation Strategy

`ReactiveUI.Validation` remains the primary validation surface for view-model state and UI feedback in the WPF app. `AllOverIt.Validation` is still worth noting as a public helper for cases where validation is not purely UI-driven and we want to register invokable validators through DI.

The package exposes two useful host-level entry points:

1. `AddValidationInvoker()` for validators that can be constructed and used as singletons.
2. `AddLifetimeValidationInvoker()` for validators that need scoped dependencies.

The validation demos also show two registration styles that may be useful if the app needs them later:

1. Auto-registering `IValidator<>` implementations through `AllOverIt.DependencyInjection` using the open-generic registration path.
2. Binding options with `AllOverIt.Validation.Options` so `AddOptions<T>()` can call `UseFluentValidation()` and validate configuration on startup.

`AllOverIt.Validation` also provides `ValidatorBase<T>` and `ValidationContextExtensions.SetContextData()` / `GetContextData()` to keep validators stateless while still passing extra context into a validation request. This is worth noting if any studio validation rule later depends on external state beyond the edited model itself.

That options-validation helper is probably less central to the WPF shell than it is to server apps, but it is still a valid fit if the studio later wants startup validation for persisted application settings or tool configuration.

### 7.11 WPF Helpers

`AllOverIt.Wpf` is more of a supporting utility package than a core application dependency, but it has a couple of useful primitives for a desktop shell:

1. `UIThread` and the related awaitable helpers make explicit UI-thread marshaling easier when a feature needs to switch contexts outside of ReactiveUI's normal scheduling.
2. `WindowWrapper` and the `WrapWindow()` extension provide direct control over window chrome, including enabling, disabling, or hiding standard caption buttons.

For `SlnDependencyStudio`, these helpers should be treated as optional implementation aids rather than the primary architecture. ReactiveUI and the WPF dispatcher still remain the main composition and UI-thread model, but `AllOverIt.Wpf` is worth keeping in mind if the shell needs more direct window control or background-thread coordination.

### 7.12 Cross-Frontend Guardrails (WPF + CLI)

Shared requirements are documented in `PRDs/v4/SlnDependencyStudio PRD - Shared Contracts.md`. If any overlap in this WPF PRD conflicts with the shared-contracts PRD or CLI PRD, implementation must pause and the conflict must be raised to the product owner for explicit alignment before proceeding.

To keep both frontends first-class and avoid parity drift, the WPF project shall follow these guardrails:

1. Shared contracts first: dependency-project schema, serialization, defaults, and migration behavior live in shared code before frontend-specific behavior is finalized.
2. Shared orchestration first: generation request/response contracts and cancellation semantics live in shared/application code and are reused by WPF and CLI.
3. Frontend boundaries: WPF-only concerns (views, visual state, interaction affordances) remain in WPF code and do not leak into shared or CLI-consumed layers.
4. Dual dependency mode discipline: local development can use project references while release validation and publishing use package references under an explicit build switch.
5. Deterministic release automation: release scripts must run a predictable build/test/package path and verify both dependency modes before tag-ready outputs are produced.
6. Sequencing safety: if implementation order ever creates risk to shared contracts, CLI/headless pipeline work may be implemented before WPF feature completion to preserve cross-frontend parity.

---

## 8. External Tooling Strategy

The current core library already uses `AllOverIt.Process` to locate required executables by calling `where` on Windows and `which` on non-Windows platforms. `SlnDependencyStudio` should follow the same pattern for consistency and should centralise that behaviour in a dedicated tool-detection service.

Recommended behaviours:

1. Detect tool presence at startup.
2. Allow manual re-scan.
3. Cache the last known tool status in memory.
4. Use the detected status to enable or disable related controls.
5. Provide a user-facing explanation when an option is unavailable because a tool is missing.

---

## 9. Recommended NuGet Packages

### Required or Strongly Recommended

| Package                         | Purpose                             | Notes                                                |
| ------------------------------- | ----------------------------------- | ---------------------------------------------------- |
| `ReactiveUI` / `ReactiveUI.WPF` | Reactive MVVM foundation            | Core stack choice.                                   |
| `ReactiveUI.Validation`         | View-model validation integration   | Best fit for ReactiveUI-based field validation.      |
| `Microsoft.Extensions.Hosting`  | Application startup/composition     | Aligns with requested DI model.                      |
| `Ookii.Dialogs.Wpf`             | Modern folder/file dialog support   | Especially useful for folder browsing in WPF.        |
| `AllOverIt.Process`             | CLI discovery and process execution | Already aligned with the current generator approach. |

### Good Candidates

| Package                     | Purpose                      | Notes                                                                        |
| --------------------------- | ---------------------------- | ---------------------------------------------------------------------------- |
| `FluentValidation`          | Declarative validation rules | Complements `ReactiveUI.Validation`; not redundant.                          |
| `Jot`                       | User-state persistence       | Useful for recent files and window state if AppData JSON storage is desired. |
| `ICSharpCode.AvalonEdit`    | Rich text/code viewer        | Useful if generated `.d2`/`.mmd` content is displayed in-app later.          |
| `DiffPlex` / `DiffPlex.Wpf` | Diff UI support              | Future-friendly if project or output diffing is added.                       |

### Packages to Avoid Unless a Clear Need Emerges

| Package                 | Reason                                                    |
| ----------------------- | --------------------------------------------------------- |
| `CommunityToolkit.Mvvm` | Overlaps heavily with ReactiveUI.                         |
| `Prism.Wpf`             | Overlaps with ReactiveUI navigation/composition patterns. |

### Theme Package Decision

The UI stack for the first release is:

1. `MahApps.Metro` for window chrome and shell-level styling.
2. `MaterialDesignThemes`
3. `MaterialDesignThemes.MahApps` to bridge both styling systems cleanly.

This is the baseline visual stack for implementation and should be treated as a project decision rather than a remaining design option.

---

## 10. Non-Functional Requirements

| ID     | Requirement                                                                                                                  |
| ------ | ---------------------------------------------------------------------------------------------------------------------------- |
| NFR-1  | The application shall be maintainable and extensible so new generator capabilities can be surfaced with minimal redesign.    |
| NFR-2  | The codebase shall follow professional engineering practices despite being a hobby project.                                  |
| NFR-3  | Runtime failures caused by missing external tools or invalid configuration shall be explicit and user-understandable.        |
| NFR-4  | The UI shall remain responsive during generation.                                                                            |
| NFR-5  | The application shall not require publishing the core generator package independently before studio development can proceed. |
| NFR-6  | The application shall persist settings and dependency project data using file-based storage.                                 |
| NFR-7  | The first release shall prioritise transparency and correctness over visual novelty or non-essential animation.              |
| NFR-8  | The first release shall support Windows 10 only.                                                                             |
| NFR-9  | Shared code consumed by WPF and CLI shall remain frontend-agnostic and avoid direct dependencies on WPF assemblies.          |
| NFR-10 | Release builds shall be reproducible through checked-in PowerShell scripts rather than ad-hoc manual command sequences.      |
| NFR-11 | Package-reference and project-reference build modes shall both remain healthy in CI.                                         |
| NFR-12 | This PRD shall remain aligned with `PRDs/v4/SlnDependencyStudio PRD - Shared Contracts.md` and `PRDs/v4/SlnDependencyStudio PRD - CLI.md` for shared schema and orchestration behavior. |

---

## 11. Initial Acceptance Criteria

1. A user can create a new dependency project from defaults.
2. A user can create a new dependency project from an existing dependency project file.
3. A user can edit the generator configuration and save the dependency project to disk.
4. A user can configure the default folder for dependency project files in application settings.
5. The application detects whether `d2` and Mermaid tooling are available and disables unsupported options accordingly.
6. The application runs generation using `SlnDependencyDiagramGenerator` through the configured dependency mode (project-reference local mode or package-reference release mode).
7. All CLI output produced during generation is visible in the UI.
8. After generation, the user can open Windows File Explorer at the export root.
9. The dependency project format includes project name and description metadata and is versioned for future growth.
10. If configured, the pre-generation command runs before diagram generation and its output is visible in the same run log.
11. While generation is in progress, configuration editing controls are disabled via ReactiveUI state binding and a cancel action remains available.
12. User cancellation stops generation without leaving the application in an inconsistent state.
13. While generation is in progress, the app cannot be closed from the main window.
14. The dependency project file written by WPF is loadable by CLI without schema-specific adaptation code in either frontend.
15. Shared generation orchestration behavior (including cancellation semantics) is consistent across WPF and CLI runs.
16. Build automation supports both local project-reference mode and package-reference release mode via explicit configuration.
17. Release PowerShell scripts produce predictable build outputs and fail fast on dependency-mode drift.
18. This WPF PRD and `PRDs/v4/SlnDependencyStudio PRD - CLI.md` both align with `PRDs/v4/SlnDependencyStudio PRD - Shared Contracts.md` for shared-contract ownership.

---

## 12. Open Questions

The following decisions are still open and should be resolved before implementation planning is finalised:

1. What settings persistence mechanism should be used: custom JSON in AppData, `Jot`, or another file-based approach?
2. Which configuration fields should be visible by default versus grouped behind collapsible or advanced sections, if any?
3. Should recent files and window state be part of the first implementation or follow soon after?
4. Which of the candidate value-add features should be promoted into the initial milestone versus deferred?
5. Should the tool include a NuGet advisory or package-upgrade feature later, or stay entirely offline/local?

---

## 13. Recommended Next Step

Create a focused implementation plan for Milestone 1 covering:

1. Prerequisite: implement full cancellation support in `SlnDependencyDiagramGenerator` and complete automated cancellation tests.
2. Studio solution scaffolding with dedicated frontend folders and shared-contract project boundaries.
3. Shared dependency-project document model, serialization, defaults, and migration behavior.
4. Shared generation orchestration contracts and service abstractions for WPF and CLI reuse.
5. Conditional dependency-mode wiring (`ProjectReference` for local development, `PackageReference` for release validation).
6. PowerShell build/release script design that enforces predictable, repeatable outputs and validates both dependency modes.
7. WPF shell layout and navigation.
8. Tool detection service.
9. Generation service with streamed output and cancellation wiring.
10. Companion CLI PRD and shared-contract PRD cross-reference alignment.

This PRD should remain a living draft while additional features are discussed.
