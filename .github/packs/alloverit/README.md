# AllOverIt Pack

This pack is synchronized from the sibling AllOverIt repository.

## Contents

- i-context/ - package index, manifest, and one package capability file per package
- 	emplates/copilot-instructions.md - ready-to-copy repository-level Copilot routing file

## Consumer Setup

Copy these files into your repository:

- packs/alloverit/ai-context/** -> .github/ai-context/alloverit/**
- packs/alloverit/templates/copilot-instructions.md -> .github/copilot-instructions.md
- instructions/alloverit-suite.instructions.md -> .github/instructions/alloverit-suite.instructions.md

## Sync Source

Source repository: sibling AllOverIt

Sync script (run from AllOverIt):

- ./_build/sync-copilot-ai-pack.ps1
