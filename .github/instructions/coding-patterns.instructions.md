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

Prefer `WhenNotNull()` over manual `ArgumentNullException` throws.

```csharp
// CORRECT
_field = value.WhenNotNull();

// WRONG
_field = value ?? throw new ArgumentNullException(nameof(value));
```

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

The `AllOverIt.Assertion` package provides two precondition mechanisms with different intended uses:

### Constructor Argument Guards (`Guard` / `WhenNotNull()`)

Use `WhenNotNull()` to guard constructor parameters. It returns the non-null value so you can assign it inline.

```csharp
// CORRECT — guards constructor injection parameters, ILogger<T> is the last parameter
public MyService(IDependency dependency, ILogger<MyService> logger)
{
    _dependency = dependency.WhenNotNull();
    _logger = logger.WhenNotNull();
}
```

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
