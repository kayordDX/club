# AI-Driven Development Recommendations

This document captures concrete steps to make the Club codebase more ergonomic for AI-assisted development: tighter linting and formatting enforcement, additional steering documents, and improved testability.

---

## 1. Linting & Formatting Enforcement

AI agents write code faster when the toolchain enforces style automatically so reviews focus on logic, not formatting.

### Frontend (already in place — gaps to close)

**Gap: no pre-commit hook**
Add `husky` + `lint-staged` so every commit is auto-formatted and lint-checked before it lands.

```bash
# from client/
pnpm add -D husky lint-staged
npx husky init
```

`.husky/pre-commit`:
```sh
cd client && pnpm lint-staged
```

`client/package.json` addition:
```json
"lint-staged": {
  "*.{ts,svelte}": ["eslint --fix", "prettier --write"],
  "*.{css,json,md}": ["prettier --write"]
}
```

**Gap: no lint step in CI**
`build-client.yml` only builds and pushes a Docker image. Add a lint/check job that runs on every PR:

```yaml
# .github/workflows/ci-client.yml
- run: pnpm lint
  working-directory: ./client
- run: pnpm check
  working-directory: ./client
```

**Gap: ESLint is not in strict TypeScript mode**
Enable `typescript-eslint` strict config in `eslint.config.js` for stronger type-safety enforcement:

```js
import ts from "typescript-eslint";
// replace ...svelte.configs.recommended with:
...ts.configs.strictTypeChecked,
```

### Backend (.NET — mostly missing)

**Add CSharpier for deterministic formatting**

```bash
dotnet tool install csharpier
# add to .config/dotnet-tools.json via: dotnet tool install --local csharpier
```

Add to `Directory.Build.props` to run on build:
```xml
<Target Name="FormatOnBuild" AfterTargets="Build" Condition="'$(CI)' != 'true'">
  <Exec Command="dotnet csharpier ." />
</Target>
```

`.github/workflows/ci-api.yml` addition:
```bash
dotnet csharpier --check .
```

**Expand `.editorconfig`**
The current file only covers indentation. Add Roslyn analyser severity levels:

```ini
[*.cs]
# Null safety
dotnet_diagnostic.CS8600.severity = error   # Converting null literal
dotnet_diagnostic.CS8602.severity = error   # Dereference of a possibly null reference
dotnet_diagnostic.CS8603.severity = error   # Possible null reference return

# Dead code
dotnet_diagnostic.IDE0051.severity = warning  # Remove unused private member
dotnet_diagnostic.IDE0052.severity = warning  # Remove unread private member

# Async
dotnet_diagnostic.CA2007.severity = suggestion  # ConfigureAwait
```

**Add a CI lint + build workflow for pull requests**
Currently `build-api.yml` only triggers on releases. Add `ci-api.yml` that triggers on PRs:

```yaml
on:
  pull_request:
  push:
    branches: [main]
jobs:
  lint-and-build:
    steps:
      - run: dotnet format --verify-no-changes
      - run: dotnet build --no-incremental -warnaserror
      - run: dotnet test Club.Tests/UnitTests/
```

---

## 2. Steering Documents

Well-placed docs give AI agents durable context so they make fewer wrong assumptions.

### `llms.txt` (root)

There is already a `.github/prompts/llms.prompt.md` prompt to update `llms.txt`, but the file itself does not exist yet. Create it following the [llms.txt spec](https://llmstxt.org/) so any LLM can quickly orient itself to the project layout.

Minimum sections to include:
- Project summary
- `## Documentation` — link README, AGENTS.md, ai.md, CONTRIBUTING.md
- `## Architecture` — link Payment.md, Wallet.md, Club.Tests/IntegrationTests/README.md
- `## Specifications` — link OpenAPI spec (swagger.json), orval.config.ts
- `## Optional` — link .github/prompts/, .agents/skills/

### `CONTRIBUTING.md` (root)

A contributor guide that covers:
- How to run the full stack (`dotnet run --project Club.AppHost/`)
- Frontend dev (`cd client && pnpm dev`)
- API client regeneration workflow (`pnpm api` in client/)
- EF migration workflow
- How to run each test suite (unit, integration, e2e)
- PR conventions (branch naming, commit messages, linking issues)
- Secret management (`dotnet user-secrets`)

### `ARCHITECTURE.md` (root)

Document the runtime topology that is only partially covered in README.md:
- Aspire AppHost orchestrates Postgres, Redis, API, and client
- Service discovery via Aspire (no hard-coded ports in code)
- OpenTelemetry wired via `Club.ServiceDefaults`
- Auth flow: Google OAuth → identity cookies + JWT → `oidc-client-ts` on frontend
- API-first: OpenAPI → orval → generated TypeScript client
- Feature-based folder structure in both API and client

### `CONVENTIONS.md` (root)

Capture decisions that are currently only in AGENTS.md but benefit from more detail:

| Area | Convention |
|---|---|
| API responses | Return typed DTO; never expose EF entities directly |
| Error handling | Use FastEndpoints `ThrowError` / `ValidationFailure`; avoid generic `Exception` for expected failures |
| HTTP status codes | 200 GET, 201 POST create, 204 delete, 400 validation, 401 auth, 404 not found |
| Async | Always `await`, never `.Result` or `.Wait()` |
| Logging | Use `ILogger<T>` injected via constructor; never `Console.Write` |
| Frontend state | `$state` for local component state; TanStack Query for server state |
| Form handling | `@tanstack/svelte-form` + Zod schemas |
| Route naming | kebab-case URL segments, match SvelteKit file-system routes |

### Architecture Decision Records (ADRs)

Create a `docs/adr/` folder with lightweight ADRs for significant past choices:

- `0001-aspire-for-orchestration.md`
- `0002-fastendpoints-over-controllers.md`
- `0003-svelte5-runes.md`
- `0004-orval-openapi-codegen.md`
- `0005-testcontainers-for-integration-tests.md`

Use a minimal template:
```markdown
# ADR-NNNN: Title

**Status:** Accepted  
**Date:** YYYY-MM-DD

## Context
Why was this decision needed?

## Decision
What was decided?

## Consequences
What are the trade-offs?
```

### `.github/prompts/` — additional prompt files

| File | Purpose |
|---|---|
| `svelte-component.prompt.md` | Scaffold a new Svelte 5 component with props, tests, and storybook-style fixture |
| `integration-test.prompt.md` | Generate an integration test for a given endpoint using the AppFixture pattern |
| `db-migration.prompt.md` | Safely add an EF migration: check existing entities, generate, validate SQL |
| `error-handling.prompt.md` | Audit an endpoint for proper FastEndpoints error responses vs raw exceptions |

---

## 3. Testability

### What is currently covered

- **Unit tests**: `Club.Tests/UnitTests/` — only `Encryption` covered
- **Integration tests**: `Club.Tests/IntegrationTests/` — Account (register + login), real Postgres via TestContainers
- **E2E tests**: Playwright is configured (`client/playwright.config.ts`) but `client/e2e/` has no test files yet

### High-value gaps to close

**Backend: extend integration test coverage**

Each feature folder in `Club.Api/Features/` (Booking, Facility, Outlet, Slot, Payment…) should have at least:
1. Happy-path test for the main operation
2. Auth-required test (unauthenticated request returns 401)
3. Validation error test (bad input returns 400)

Use the existing `AppFixture` pattern — no new infrastructure needed.

**Backend: test data builders**

Add a `Club.Tests/IntegrationTests/Builders/` folder with fluent builders for common entities so tests read clearly:

```csharp
var booking = new BookingBuilder()
    .WithFacility(facilityId)
    .WithSlot(slotId)
    .WithUser(userId)
    .Build();
```

**Backend: fix error handling so tests can assert correct status codes**

The `IntegrationTests/README.md` notes that several validation errors currently return 500 instead of 4xx. Replace raw `throw new Exception(...)` in endpoints with `FastEndpoints` `ThrowError` / `AddError` + `ThrowIfAnyErrors()`. This makes tests deterministic and meaningful.

**Frontend: E2E tests for critical paths**

Create at minimum:
- `client/e2e/booking-flow.spec.ts` — guest can view slots and start a booking
- `client/e2e/login.spec.ts` — member login redirects correctly
- `client/e2e/auth-guard.spec.ts` — protected routes redirect unauthenticated users

Playwright is already installed; just add spec files and ensure `pnpm test:e2e` runs in CI.

**Frontend: component unit tests**

Co-locate tests with components (`.svelte.test.ts`). Prioritise:
- Form components (booking form validation logic)
- Shared layout components
- Any component that has conditional rendering logic

**Backend: unit test coverage**

Add unit tests for:
- Domain/business logic helpers in `Common/`
- Validation rules for complex request models
- Any utility/service class that doesn't need a database

---

## 4. Making It All AI-Runnable

AI agents can only reliably execute what is scripted. Make every quality gate a one-liner.

### Recommended VS Code tasks additions (`.vscode/tasks.json`)

Add tasks that agents can invoke:
- `lint:all` — runs `dotnet format --verify-no-changes` + `pnpm lint` + `pnpm check`
- `test:all` — runs unit tests, integration tests, and E2E tests
- `api:regenerate` — runs `pnpm api` in `client/` to regenerate the OpenAPI client
- `db:migrate` — runs `dotnet ef database update`

### `mise.toml` additions

The repo already uses `mise` for toolchain management. Pin exact versions for all tools AI agents use:

```toml
[tools]
dotnet = "10.x"
node = "22.x"
pnpm = "10.x"
"dotnet:csharpier" = "latest"
```

### AI agent context checklist

When an agent starts a task it should be able to answer all of these from docs alone:

- [ ] How do I start the full stack locally?
- [ ] How do I run all tests?
- [ ] How do I add a new API endpoint? (→ `endpoint.prompt.md`)
- [ ] How do I add a new database migration? (→ `db-migration.prompt.md`)
- [ ] How do I regenerate the API client after changing an endpoint?
- [ ] What HTTP status codes should I use?
- [ ] How do I handle errors in an endpoint?
- [ ] Where do new frontend components go?
- [ ] Where do tests go and how do I name them?

If any answer requires reading source code instead of a doc, write the doc.

---

## Priority Order

| Priority | Action | Effort |
|---|---|---|
| 1 | Add CI lint job for both API and client on PRs | Low |
| 2 | Create `llms.txt` at root | Low |
| 3 | Create `CONTRIBUTING.md` | Low |
| 4 | Install CSharpier and add to CI | Low |
| 5 | Add pre-commit hook (husky + lint-staged) | Low |
| 6 | Fix 500 error handling in endpoints → enables meaningful tests | Medium |
| 7 | Expand integration test coverage to all feature areas | Medium |
| 8 | Add E2E Playwright tests for critical booking + auth flows | Medium |
| 9 | Create `ARCHITECTURE.md` and `CONVENTIONS.md` | Medium |
| 10 | Add test data builders | Medium |
| 11 | Write initial ADRs | Low |
| 12 | Add additional `.github/prompts/` files | Low |
