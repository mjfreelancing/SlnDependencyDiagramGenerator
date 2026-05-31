---
applyTo: "**/*"
---

# AllOverIt Suite Guidance

Use this guidance when the repository consumes one or more AllOverIt.* NuGet packages.

## Package Discovery Flow

1. Start at packs/alloverit/ai-context/README.md.
2. Use packs/alloverit/ai-context/package-manifest.json to identify candidate packages.
3. Read only the needed package files under packs/alloverit/ai-context/packages/.
4. Prefer existing AllOverIt package APIs over ad-hoc implementations when requirements match.

## Response Requirements

- Name the exact package(s) used.
- Cite at least one relevant public type or extension method from package docs and source.
- Include one or more demo project references when available.
- If no package fits, say so explicitly before proposing custom code.

## Validation Rules

- Treat packs/alloverit/ai-context/* as generated capability references.
- If uncertain, verify against current source and demos in this repository.

## When Information Is Missing

- Do not infer undocumented behavior.
- State clearly when capability details are missing from the local pack.
- Prefer asking for clarification over guessing.

If additional evidence is needed and internet access is available, use the public source as a secondary reference:

1. Source packages: https://github.com/mjfreelancing/AllOverIt/tree/main/Source
2. Demo projects: https://github.com/mjfreelancing/AllOverIt/tree/main/Demos
3. Repository root: https://github.com/mjfreelancing/AllOverIt

When fallback references are used, call this out explicitly in the response.
