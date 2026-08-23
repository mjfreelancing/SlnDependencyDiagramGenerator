---
applyTo: "**/*"
---

# Language-Agnostic Core Instructions

## Core Rules

### Collaboration and Scope

- When implementing a feature or refactoring existing code, stay focused on the requested scope; do not make unrelated changes. If scope is unclear, ask for clarification or provide a recommendation before changing anything outside the request.
- When troubleshooting, present options and ask the user to choose; do not automatically pick what seems best.

### Naming and Readability

- Use clear names and cohesive feature-level organization.
- Avoid one-letter variable names, including variables used in expressions (for example, use `order => order.CalculateTotal()` instead of `x => x.CalculateTotal()`). If the element meaning is not obvious (beyond simple cases like iterating strings), prefer `item`.
- Do not remove existing comments. If a comment appears out of date and needs correction, ask the user to confirm before changing it. Simple spelling corrections are allowed.
- In multi-line boolean expressions, place logical operators (`&&`, `||`) at the end of the preceding line, not at the start of the next line.
- Keep code units focused and avoid unnecessary abstraction.
- Avoid inline construction of model/request objects in method call arguments. Extract to a named variable so the intent is explicit and the call site remains readable.

  ```csharp
  // CORRECT:
  var request = new ParseRequest { Path = solutionPath, MaxDepth = 3 };
  var result = await parser.ParseAsync(request, cancellationToken);

  // WRONG:
  var result = await parser.ParseAsync(new ParseRequest { Path = solutionPath, MaxDepth = 3 }, cancellationToken);
  ```

### Reuse and Boundaries

- Make an explicit attempt to find existing code that can be reused before introducing new implementations.
- If code may be better placed in shared helpers/utils or a separate shared project/package, consult the user on preferred location before creating or moving it. Do not assume location. This applies to production code and unit/integration test projects.
- Keep responsibilities separated by concern (for example transport, orchestration, domain/application logic, persistence, and UI state).
- Reuse shared helpers before introducing new variants.
- Avoid cyclic dependencies and hidden side effects.

### Contracts and Runtime Safety

- Preserve public API shape unless change is explicitly requested.
- Keep boundary contracts stable and versioned where applicable.
- Handle success/failure outcomes explicitly.

## Precedence

When a rule in this file conflicts with a language-specific instruction file, the language-specific file takes precedence.

## Expansion Notes

- Keep cross-language baseline rules in this file.
- Keep language- or framework-specific rules in scoped instruction packs.
