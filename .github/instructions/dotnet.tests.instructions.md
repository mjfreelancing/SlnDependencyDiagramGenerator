---
applyTo: "**/*Tests/**/*.cs"
---

# .NET Test Instructions

## Core Rules

### Scope and Isolation

- Keep tests deterministic and isolated from external systems.
- Prefer behavior and contract assertions over implementation details.
- Keep unit and integration tests separated by project responsibility.
- Keep tests in the nearest layer-specific test project.

### Conventions

- Use fixture-style test classes and verb-first test method names.
- Prefer outer test classes named `<Type>Fixture` and nested classes grouped by the method or scenario under test.
- **XML documentation is not required in test files.** The general C# XML documentation rules (`csharp.instructions.md`) do not apply to test code. Only add XML docs to test helpers when the logic is genuinely non-obvious.
- Name test methods with a `Should_...` prefix and sentence-style underscores, for example `Should_Return_No_Validation_Errors` or `Should_Throw_When_Input_Is_Invalid`.
- Keep xUnit attributes (`[Fact]`, `[Theory]`) but do not force xUnit's default naming style when a clearer repository convention exists.
- Keep test method order aligned with the implementation logic order when practical (for example, log/assert-first tests should appear before later-branch tests in the same fixture).
- Keep assertion style consistent within each test project.
- Keep reusable helpers in a shared test utility project when they are cross-project.
- When setup or assertion patterns repeat, prefer adding or extending shared helpers rather than duplicating logic in individual fixtures.
- Keep shared test utility projects strictly general-purpose; project-specific helpers should remain in the owning test project.
- When internal visibility is needed for testing, use csproj `InternalsVisibleTo` declarations.
- For targeted reruns, prefer project-level execution with `--filter` on fully-qualified test names.

### Placement

- Unit tests: project-level test projects that validate in-process behavior.
- Integration tests: hosted API or end-to-end boundary tests with real HTTP/database boundaries and real `HttpClient` requests.

### Snapshot Testing

- When a test asserts on a generated string output (diagram text, serialized JSON, formatted reports, multi-line templates, etc.), prefer a Verify snapshot over multiple `ShouldContain`/`ShouldBe` assertions. A single `await Verifier.Verify(content)` replaces all manual string checks.
- Keep Verify snapshots in a `Snapshots` subdirectory within each test project. Configure the directory via `[ModuleInitializer]` with `Verifier.UseProjectRelativeDirectory("Snapshots")`.
- Snapshot test methods must be `async Task` (not `void`) and call `await Verifier.Verify(content)`.
- Use `VerifierSettings.AddScrubber(...)` in the module initializer to normalize machine-specific content (e.g. absolute paths, timestamps) that would make snapshots non-deterministic across environments.
- After verifying the initial received output, approve snapshots by renaming `.received.txt` to `.verified.txt` and committing them to source control. Subsequent test runs compare against these committed verified files.
- Do not mix snapshot assertions with manual string assertions in the same test — choose one approach.

### Layer-Specific Expectations

- Keep test-only dependencies in test projects; do not add them to production projects.
- Aim for full coverage of behavior and meaningful branch paths for the production code under test; when coverage gaps are discovered, add targeted tests to close them.

## Expansion Notes

- Keep framework/tooling choices in project-level docs if needed.
- Add layer-specific conventions in dedicated scoped files.
