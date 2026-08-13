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

## Environment

- First run: `pnpm install` in `client/` — fresh worktrees/checkouts have no `node_modules`; install before `pnpm check`/lint or before verifying package exports. `pnpm check` passing also confirms named imports from `@kayord/ui`/`@lucide/svelte` exist (no need to inspect the package manually)
- `pnpm api` and integration tests (Testcontainers) require Docker + running stack
- Without Docker/stack: integration tests compile only; hand-edit generated client to match orval output (note this in the PR)

## General Guidelines

- Do not add comments to explain everything. Add comments only where it will add real value.

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

## Tests

When any bigger code changes were made make sure to run the tests and verify they pass.
If you are adding new functionality, make sure to add tests for it.

```sh
# Backend tests: run from root folder
dotnet test
# Client tests: run from client folder
pnpm test   # make sure tests pass
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
- Use tanstack svelte-form (`createAppForm` in `$lib/components/Form`) for forms
- All `goto()`/`href` must use `resolve()` or be typed `ResolvedPathname` (`svelte/no-navigation-without-resolve`)
- Shared logic/components across routes go in `$lib/` — don't deep-import across route trees
- `@kayord/ui` facts: `Alert.Root` variants are only `default` | `destructive`; `Button href` accepts `ResolvedPathname`; check `node_modules/@kayord/ui/dist/components/ui/<name>/` for variant defs
- Booking domain: `BookingDTO` (from `createBookingGet`) already includes `user`, `extraBookings[].extra` (name/price), and `slotContractBookings[].slotContract` (`startDatetime`, `facilityId`, `contractName`, `price`). Facility/outlet names come from `createBookingGetPath` → `BookingPathDTO` (`GET /booking/{id}/path`) — do NOT add facility/outlet to `BookingDTO`. Booking pages that show summary details use this pattern (see edit/view/pay pages, `BookingBreadcrumbs`, `getBookingPayUrl`). Shared booking UI/helpers: players & extras tables = `$lib/components/BookingPlayers.svelte` / `BookingExtras.svelte`; en-ZA formatters = `$lib/booking/format.ts` (`formatCurrency`/`formatDate`/`formatTime`/`formatDateTime`) — reuse these instead of redefining per page
- For UI components, use the `ui` skill

### Backend (C#/.NET)

- File-scoped namespaces
- Feature-based layout under `Club.Api/Features/`
- Entities: singular, PascalCase; DTOs in `DTO/`
- Never commit secrets — use `dotnet user-secrets` locally
- Use the `api` skill for endpoint and service patterns

### Testing

- Frontend unit: `ComponentName.svelte.test.ts` colocated with component
- Frontend E2E: `feature-name.spec.ts` in `client/e2e/` — runs against the Aspire-started stack (see E2E workflow below)
- Backend: `ClassNameTests.cs` — xUnit, arrange-act-assert
- Target a single backend test class: `dotnet test Club.Tests/IntegrationTests/IntegrationTests.csproj -- --filter-class <FullyQualifiedClassName>`
- FastEndpoints.Testing client: `POSTAsync`/`GETAsync<TEndpoint,TReq,TRes>` returns `(HttpResponseMessage, TRes?)` tuple; `PUTAsync<TEndpoint,TReq>` returns `HttpResponseMessage` directly

### E2E Tests (Playwright + Aspire)

1. From repo root: `aspire start` — brings up Postgres, Redis, Keycloak, Mailpit, API, and the SvelteKit dev server (port 5173)
2. From `client/`: `pnpm test:e2e` — Playwright runs against `http://localhost:5173`
3. Stop the stack: `aspire stop`

The stack must be running first — `client/playwright.config.ts` has a `globalSetup` that fails fast with instructions if the frontend is unreachable. The browser never talks to the API directly; the SvelteKit server proxies API calls via `API_URL` (`$lib/server/api/client.ts`). CI: `.github/workflows/test-e2e.yml` (manual `workflow_dispatch` only — starts the AppHost with cached container images, waits on the `web` resource, runs Playwright, uploads report + Aspire logs on failure).

## Key Workflows

### New API Endpoint

1. Create endpoint in `Club.Api/Features/{FeatureName}/`
2. Register services in `Common/Extensions/` if needed
3. Regenerate frontend API client from `client/` (requires running API). Hand-edits must mirror orval output exactly (see `paymentCheckout`/`facilityGet` in `generated/`). If `swagger.json` is empty, something is wrong — revert it
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

| Item             | Value                                                                                                              |
| ---------------- | ------------------------------------------------------------------------------------------------------------------ |
| Run stack        | `aspire start`                                                                                                     |
| View logs        | `aspire logs`                                                                                                      |
| API docs         | `http://localhost:5000/scalar/v1`                                                                                  |
| Aspire           | `use aspire skills to get details and logs`                                                                        |
| Set secret       | `dotnet user-secrets set "Key" "Value"`                                                                            |
| EF migrations    | VS Code tasks or `dotnet ef` CLI                                                                                   |
| API client regen | Requires running API: `pnpm api` from `client/` (mirror orval output if hand-editing; revert empty `swagger.json`) |
