# Product Requirements Document - SlnDependencyStudio Shared Contracts

**Date:** May 2026  
**Status:** Draft  
**Scope:** Authoritative cross-frontend contracts for SlnDependencyStudio, shared by WPF and CLI frontends

**Companion PRDs:**

1. `PRDs/v4/SlnDependencyStudio PRD - WPF.md`
2. `PRDs/v4/SlnDependencyStudio PRD - CLI.md`

---

## 1. Purpose

This document centralizes requirements that apply to both frontends.

If any shared requirement appears to conflict between this document and a frontend PRD, or between frontend PRDs, implementation must pause and the conflict must be raised to the product owner for explicit alignment before work proceeds.

---

## 2. Cross-Frontend Delivery Contract

1. WPF and CLI are first-class deliverables for the same release train.
2. Implementation order can vary, but shared parity cannot be compromised.
3. If parity risk appears, shared or CLI headless pipeline work may be prioritized before additional WPF features.

---

## 3. Shared Solution and Project Boundaries

1. The studio solution must reserve room for multiple frontends.
2. WPF frontend lives under a dedicated WPF folder/project.
3. CLI frontend lives under a dedicated CLI folder/project.
4. Shared contracts and orchestration-facing models live in shared frontend-agnostic projects.
5. Shared projects must not depend on WPF assemblies.

---

## 4. Dependency Project Document Contract

1. Dependency project files are cross-frontend artifacts.
2. Schema versioning, serialization, defaults, and migration behavior are shared responsibilities.
3. Unknown fields should be ignored where practical to preserve forward compatibility.
4. WPF-authored files must execute in CLI without frontend-specific adaptation code.
5. Drift in schema behavior between frontends is a release blocker.

---

## 5. Shared Generation Orchestration Contract

1. Generation workflow logic runs through shared application services consumed by both frontends.
2. Pre-generation command execution behavior is shared.
3. Continue-on-failure semantics for pre-generation command are shared.
4. End-to-end cancellation behavior is shared.
5. Cancellation support in `SlnDependencyDiagramGenerator` with automated tests is a prerequisite.

---

## 6. External Tool Detection Contract

1. Resolution order is shared: explicit override path, then PATH lookup via `AllOverIt.Process`, then additional supported mechanisms.
2. Cross-platform behavior must be respected in shared and CLI flows.
3. Missing required tools must be surfaced clearly and deterministically.

---

## 7. DI and Configuration Contract

1. DI registration is grouped by feature-focused extension methods.
2. `AllOverIt.DependencyInjection` conventions apply across shared/backend services, including `ServiceRegistrarBase`, `AutoRegisterTransient`, `AutoRegisterScoped`, and `AutoRegisterSingleton`.
3. Marker interfaces may be used for lifetime selection.
4. Marker interfaces must be filtered out during auto-registration.
5. When ISP causes unintended interface registrations, filter generic interfaces explicitly.
6. Use reusable options binding helpers (for example `AddSingletonFromOptions<T>()`) to avoid repeated boilerplate.

---

## 8. Validation Contract

1. Shared deterministic validation rules live in shared/application code.
2. Frontend validation presentation can differ, but shared validation outcomes for the same input must match.
3. FluentValidation is allowed and recommended where appropriate.
4. `AllOverIt.Validation` helpers (`AddValidationInvoker`, `AddLifetimeValidationInvoker`, context-based helpers) may be used where they reduce duplication.
5. `AllOverIt.Validation.Options` may be used for startup/options validation when valuable.

---

## 9. Build Mode Contract

1. Build configuration supports conditional dependency modes (for example `UseLocalGeneratorProjectRefs`).
2. Local development mode may use `ProjectReference` to local generator source.
3. Release validation mode must support `PackageReference` to generator packages.
4. CI must validate both modes continuously.
5. Drift between modes is a release blocker.

---

## 10. Release Automation Contract

1. Release builds are driven by checked-in PowerShell scripts.
2. Scripts must set and validate dependency mode explicitly.
3. Scripts must run predictable build/test/package paths.
4. Scripts must fail fast on parity or dependency-mode drift.
5. Release outputs should be deterministic and tag-ready.

---

## 11. Global Tool Readiness Contract

1. CLI architecture must remain compatible with `.NET tool` packaging.
2. Tool packaging is permitted once command surface and compatibility tests are stable.
3. Packaging metadata and release scripts must be deterministic.
4. Global tool documentation must clearly explain install/update/version behavior.

---

## 12. Testing and Parity Contract

1. Shared golden dependency-project files should be used to verify parity.
2. Equivalent scenarios should produce equivalent shared outcomes across WPF and CLI.
3. Cross-frontend parity checks are part of release validation.

---

## 13. Update Protocol

1. Shared requirement change: edit this document first.
2. Frontend impact: update WPF and CLI PRDs to reference the revised shared requirement.
3. Any frontend-specific exception must be explicitly documented as an exception in the frontend PRD.
4. If conflicting wording is discovered across the three PRDs, do not infer precedence; raise the conflict to the product owner and resolve it explicitly in all affected documents.

This document should remain a living draft while both frontends and shared contracts evolve.
