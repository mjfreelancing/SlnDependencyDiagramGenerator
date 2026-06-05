---
applyTo: "**/*.cs"
---

# C# Instructions

## Core Rules

### Language and Code Quality

- Treat `.editorconfig` as the source of truth for formatting and analyzer style rules.
- Use clear naming and cohesive feature-level organization.
- Avoid one-letter variable names unless loop/index intent is obvious.
- Keep methods focused and avoid unnecessary abstraction.
- Keep method and constructor signatures compact: start on one line and fit as many parameters as comfortable; wrap at a natural breaking point (after a comma or after the opening parenthesis) when the line would become too long. Continuation lines should be indented one level.
- Follow member-ordering conventions consistently within each project:
  - Place nested types above constants, fields, and properties.
  - Place constants before other fields; place static readonly fields before instance fields.
  - Place private readonly dependency fields above other instance fields.
  - Place properties above constructors.
  - Keep constructors above methods.
  - Order methods by visibility: public, protected, internal, private.
- Avoid sync-over-async (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`).
- Keep `CancellationToken` propagation intact in async chains.
- Place extension methods in an `Extensions` folder and name extension files/classes after the extended type (`<ExtendedType>Extensions`).
- Preserve public API shape unless change is explicitly requested or approved.
- Implement with testability in mind; prefer constructor injection and avoid static dependencies.
- Default to sealed classes unless extensibility is required.
- Do not use primary constructors; prefer explicit constructor definitions for clarity and testability.

### XML Documentation

- Add XML doc comments to all `public` and `internal` types (classes, interfaces, records, structs, enums) and their members, unless the member is an explicit interface implementation (use `<inheritdoc />` there instead).
- Document all `protected` members, as they form part of the API available to derived classes. Do not document `private` members unless the logic is genuinely non-obvious and a comment would be clearer as a doc comment than an inline remark.
- Use `<inheritdoc />` on any method, property, or type that fully implements or overrides a documented base/interface member. Add extra `<remarks>` only when the override meaningfully diverges from the contract.
- Keep `<summary>` text concise — one or two sentences that describe _what_ the member does, not _how_. Avoid restating the member name.
- Write `<param>` entries for every parameter; omitting them will trigger analyser warnings.
- Write `<returns>` when the return type alone does not communicate what the value represents (e.g. a `bool` whose meaning is ambiguous, or a complex result type). Omit it for `void` and for trivially named return types.
- Use `<see cref="..." />` to cross-reference related types or members. Prefer `cref` over repeating a type name as plain text.
- Use `<see langword="..." />` for C# keywords appearing in documentation text (e.g. `<see langword="null" />`, `<see langword="true" />`, `<see langword="false" />`, `<see langword="async" />`).
- Use `<remarks>` for context that does not belong in `<summary>`: threading notes, side effects, interaction with other members, or rationale for a non-obvious design decision.
- Use `<exception cref="..." />` on `public` and `internal` methods that deliberately throw (not for exceptions thrown deep in a call chain).
- Do not add documentation solely to satisfy an analyzer warning on auto-generated, trivial boilerplate, or test helper code; suppress the warning instead if documentation adds no value.
- Enum members should each carry a `<summary>` describing what the value represents.

### C# Architecture

- Keep handlers thin: validate, map input, call service, map result.

### Constructor and Method Signature Ordering

- When injecting services via constructor, the logging interface (when required) is always the **last** injected parameter.
- The `private readonly` backing fields for injected services must be listed in the **same order** as the corresponding constructor parameters.
- For method calls, `CancellationToken` (when required) is always the **last** argument, unless a language-enforced positional constraint prevents it (e.g. `[CallerMemberName] string callerName = ""` must appear after the token).

## Expansion Notes

- Baseline cross-language rules live in `language-agnostic-core.instructions.md`.
- Keep C#-specific conventions in this file.
- Move project- or layer-specific rules to additional scoped instruction files.
