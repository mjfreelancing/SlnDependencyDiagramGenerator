---
applyTo: "**/*.cs"
---

# Coding Patterns & Guidelines

> **Living document.** This file is updated as new patterns are identified or mistakes are corrected.
> As content grows, related items may be grouped into categorized sections or extracted into dedicated instruction files.

This file extends [`csharp.instructions.md`](csharp.instructions.md) and [`language-agnostic-core.instructions.md`](language-agnostic-core.instructions.md).
All rules from those files apply here.

## AllOverIt Extension Method Preferences

These rules apply when the `AllOverIt.Extensions` namespace is available (via the `AllOverIt` NuGet package).

### String Empty / Null Checks

Prefer AllOverIt extension methods over the BCL `string.IsNullOrEmpty()` / `string.IsNullOrWhiteSpace()` family.

```csharp
// CORRECT — uses AllOverIt extension methods
if (value.IsNullOrEmpty())
if (value.IsNotNullOrEmpty())

// WRONG — uses BCL static methods
if (string.IsNullOrEmpty(value))
if (!string.IsNullOrEmpty(value))
if (!string.IsNullOrWhiteSpace(value))
```

`IsNullOrEmpty()` replaces `string.IsNullOrEmpty(value)`.
`IsNotNullOrEmpty()` replaces both `!string.IsNullOrEmpty(value)` and `!string.IsNullOrWhiteSpace(value)`.

### Null Argument Guards

Use `WhenNotNull()` when guarding values that can legitimately be `null` at runtime — for example, method arguments supplied by callers. Prefer it over manual `ArgumentNullException` throws.

```csharp
// CORRECT — guards a runtime method argument
filePath.WhenNotNull();

// WRONG — manual throw
if (filePath is null) throw new ArgumentNullException(nameof(filePath));
```

Constructor injection parameters do **not** need null guards — the DI container resolves every dependency and throws if one cannot be resolved, so a `null` is never injected (see the Constructor Arguments section below).

## AllOverIt.Validation Patterns

When using `AllOverIt.Validation` for FluentValidation integration:

- Inherit from `ValidatorBase<T>` (AllOverIt), not directly from `AbstractValidator<T>` (FluentValidation).
- Call `DisablePropertyNameSplitting()` in the **static constructor**.
- Use `IsNotEmpty()` for required string rules instead of `NotEmpty()`.

```csharp
// CORRECT
internal sealed class MyValidator : ValidatorBase<MyModel>
{
    static MyValidator()
    {
        DisablePropertyNameSplitting();
    }

    public MyValidator()
    {
        RuleFor(model => model.Name).IsNotEmpty();
    }
}
```

## Precondition Checks with `Throw<>`

The `AllOverIt.Assertion` package provides `WhenNotNull()` and `Throw<TException>` with different intended uses:

### Constructor Arguments (DI-injected)

Constructor injection parameters do **not** require null guards. The DI container resolves every dependency from registered services and throws if a service cannot be resolved, so a `null` can never be injected.

```csharp
// CORRECT — DI resolves all dependencies; no guards needed. ILogger<T> is the last parameter.
public MyService(IDependency dependency, ILogger<MyService> logger)
{
    _dependency = dependency;
    _logger = logger;
}
```

Keep a `WhenNotNull()` guard on a constructor parameter only when the class is created outside the container by a factory method that may pass `null`.

### Dependent-State Checks (`Throw<TException>`)

Use `Throw<TException>` to validate **dependent state** that should have been established before a method runs — typically properties set by DI or code-behind wiring after construction. These are not constructor parameters, so `WhenNotNull()` does not apply.

```csharp
// CORRECT — validates dependent state before proceeding
public async Task SaveAsync()
{
    Throw<InvalidOperationException>.WhenNull(
        SettingsEditorViewModel,
        $"The {nameof(SettingsEditorViewModel)} has not been set.");

    SettingsEditorViewModel.ApplyToSettings(_settingsService.CurrentSettings);
    await _settingsService.SaveAsync();
}
```

Available `Throw<TException>` methods (all in namespace `AllOverIt.Assertion`):

| Method                       | Throws when                   |
| ---------------------------- | ----------------------------- |
| `When(condition)`            | condition is `true`           |
| `WhenNot(condition)`         | condition is `false`          |
| `WhenNull(object)`           | object is `null`              |
| `WhenNotNull(object)`        | object is not `null`          |
| `WhenNullOrEmpty(string)`    | string is `null` or empty     |
| `WhenNotNullOrEmpty(string)` | string is not `null` or empty |

Each method has overloads accepting up to 4 exception constructor arguments, both as direct values and as `Func<>` delegates for deferred evaluation.

```csharp
// Direct arguments
Throw<ArgumentException>.When(value < 0, nameof(value), "Value must be non-negative.");

// Deferred arguments (avoid eager formatting in the happy path)
Throw<InvalidOperationException>.WhenNull(someObject, () => $"Unexpected null for {nameof(someObject)}.");

// Conditional check
Throw<InvalidOperationException>.WhenNot(_isInitialized, "Service has not been initialized.");
```

## Exception Logging

Prefer structured logging with the exception overload — it lets the logging framework (Serilog, etc.) decide how much detail to render based on the configured sink and level, rather than forcing the full stack trace into the message string.

```csharp
// PREFERRED — structured logging; stack trace rendering is controlled by log configuration
logger.LogError(exception, "Failed to process the request.");
```

Avoid calling `exception.ToString()` in general-purpose or production code. It forces the full stack trace into the message string unconditionally, which can leak internal implementation details (file paths, type names, call chains) into log output. Reserve this for exceptional cases where the full chain is genuinely needed and the environment is known to be safe (e.g., internal development tools or fatal startup failures).

```csharp
// AVOID in production/general code — forces full stack trace into every log output
logger.LogError("{ExceptionText}", exception.ToString());
```

Use `exception.Message` only for well-known, shallow exceptions where the type alone communicates the problem (e.g., validation errors, configuration binding failures).

```csharp
// ACCEPTABLE — for well-known, shallow exceptions
logger.LogError("{ErrorMessage}", exception.Message);
```

## Expansion Notes

- Add project- or layer-specific patterns to dedicated instruction files.
- Move patterns into categorized sub-sections or separate files as the collection grows.
