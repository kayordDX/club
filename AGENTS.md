# AGENTS.md - Coding Agent Instructions

## Project Overview

Aspire orchestrated monorepo: public booking and member login system with Google OAuth.

| Directory               | Purpose                                                                         |
| ----------------------- | ------------------------------------------------------------------------------- |
| `Club.AppHost/`         | Aspire AppHost — orchestrates API, frontend, Postgres, Redis, Keycloak, Mailpit |
| `Club.Api/`             | .NET 10 FastEndpoints backend                                                   |
| `Club.ServiceDefaults/` | Aspire service defaults (OTel, health checks, service discovery)                |
| `Club.Tests/`           | Integration and unit tests                                                      |
| `client/`               | SvelteKit 5 frontend (`client/src/`)                                            |

## Available Skills

Always invoke the relevant skill before working in that area.

| Skill                       | When to use                                                                  |
| --------------------------- | ---------------------------------------------------------------------------- |
| `ui`                        | `@kayord/ui` / shadcn-svelte components, forms, dialogs, dropdowns           |
| `api`                       | FastEndpoints, backend conventions, EF patterns, `Club.Api/Features/`        |
| `svelte-core-bestpractices` | Svelte 5 patterns, reactivity, composition, styling, performance             |
| `svelte-code-writer`        | Any `.svelte`, `.svelte.ts`, or `.svelte.js` file — lookup and code analysis |

## Quality Gates (mandatory — run after every change)

### Frontend

```sh
pnpm check   # type-check
pnpm lint    # lint
pnpm format  # format
```

### Backend

```sh
dotnet build        # build
csharpier check .   # check formatting
csharpier format .  # fix formatting if needed
```

## Code Style

### Formatting

- Frontend: tabs; Backend: 4 spaces
- Line length: 160 characters
- Trailing commas: ES5; Quotes: double
- File names: kebab-case; Classes/components: PascalCase

### Frontend (TypeScript/Svelte)

- Svelte 5 runes are **mandatory** — use `$state`, `$derived`, `$props`, `$effect`. Use the `svelte-core-bestpractices` skill for patterns.
- Use generated API clients from `client/src/lib/api/generated/`
- Custom fetch/mutator logic lives in `client/src/lib/api/mutator/customInstance.svelte.ts`
- Use `@tanstack/svelte-query` (`createQuery`, `createMutation`) for data fetching
- For UI components, use the `ui` skill

### Backend (C#/.NET)

- File-scoped namespaces
- Feature-based layout under `Club.Api/Features/`
- Entities: singular, PascalCase; DTOs in `DTO/`
- Never commit secrets — use `dotnet user-secrets` locally
- Use the `api` skill for endpoint and service patterns

### Testing

- Frontend unit: `ComponentName.svelte.test.ts` colocated with component
- Frontend E2E: `feature-name.spec.ts` in `client/e2e/`
- Backend: `ClassNameTests.cs` — xUnit, arrange-act-assert
- Target a single backend test class: `dotnet test Club.Tests/IntegrationTests/IntegrationTests.csproj -- --filter-class <FullyQualifiedClassName>`

## Key Workflows

### New API Endpoint

1. Create endpoint in `Club.Api/Features/{FeatureName}/`
2. Register services in `Common/Extensions/` if needed
3. Regenerate frontend API client from `client/`
4. Update frontend usage

→ Use the `api` skill for patterns.

### New Page or Component

1. Create file in `client/src/routes/` or `client/src/lib/components/`
2. Use Svelte 5 runes; import from `$lib/api/generated`
3. Add colocated tests

→ Use `svelte-core-bestpractices`, `svelte-code-writer`, and `ui` skills.

### Database Change

1. Modify entity in `Club.Api/Entities/`
2. Add EF migration
3. Apply migration

## Quick Reference

| Item             | Value                                                   |
| ---------------- | ------------------------------------------------------- |
| Run stack        | `dotnet run --project Club.AppHost/Club.AppHost.csproj` |
| API docs         | `http://localhost:5000/scalar/v1`                       |
| Aspire           | `use aspire skills to get details and logs`             |
| Set secret       | `dotnet user-secrets set "Key" "Value"`                 |
| EF migrations    | VS Code tasks or `dotnet ef` CLI                        |
| API client regen | Run from `client/` after API changes                    |

## Svelte and MCP Guidance

- Always check existing code in the feature directory before writing new patterns
- Use Svelte MCP tools: `list-sections` → `get-documentation`
- Run `svelte-autofixer` on all Svelte code before returning it
- Never generate a playground link unless explicitly asked
