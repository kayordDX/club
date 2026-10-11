# Agent Instructions

## Skills

Before working in an area with a repository skill, read the matching `.agents/skills/<name>/SKILL.md`. Skills are in this worktree; do not substitute global copies. See [Project Reference](docs/agent-project-reference.md) for project-specific paths, workflows, commands, and domain facts.

| Work | Skill |
| --- | --- |
| FastEndpoints, API, EF, backend | `api` |
| Aspire lifecycle, diagnostics, or AppHost | `aspire` and the relevant Aspire sub-skill |
| `.svelte`, `.svelte.ts`, `.svelte.js` | `svelte-core-bestpractices`, `svelte-code-writer` |
| `@kayord/ui` components | `ui` |
| TanStack Form / Zod forms | `tanstack-form` |

Give delegated agents the worktree path and explicit skill paths.

## General

- Prefer focused checks while iterating; run relevant quality gates and tests before handoff. For docs-only changes, verify referenced paths and run `git diff --check`.
- Capture verbose check/test output outside the repository; inspect failures and summaries.
- Add comments only when they provide real value. Never commit secrets.

## Frontend

- Use Svelte 5 runes and the repository's remote-function data-access pattern; do not call generated browser fetch helpers directly.
- Use `resolve()` or a typed `ResolvedPathname` for navigation.
- Put shared code in `$lib/`; do not deep-import across route trees.
- Use `@kayord/ui` for shared UI and `createAppForm` for forms. Follow the relevant skills for component and form patterns.

## Backend

- Use file-scoped namespaces and feature-based organization under `Club.Api/Features/`.
- Propagate cancellation tokens through async operations.

## Tests

- Add tests for new functionality. Backend tests must pass the test context cancellation token to async operations; keep builds free of `xUnit1051` warnings.
- Use the focused backend test command in the Project Reference when targeting one integration-test class.
