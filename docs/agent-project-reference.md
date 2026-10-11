# Project Reference for Agents

Load this when the task needs project layout, commands, or domain-specific conventions. Task-specific procedures belong in the relevant skill.

## Project Layout

- `Club.AppHost/`: Aspire orchestration for API, frontend, Postgres, Redis, Keycloak, and Mailpit.
- `Club.Api/`: .NET 10 FastEndpoints backend.
- `Club.ServiceDefaults/`: Aspire service defaults, telemetry, and health checks.
- `Club.Tests/`: backend tests.
- `client/`: SvelteKit 5 frontend.

## Environment and Workflows

- Fresh frontend checkout: run `pnpm install` in `client/` before checks or package-export verification.
- Before starting/restarting Aspire, inspect `aspire describe --non-interactive`; reuse an existing stack and rebuild only changed resources. Use `--isolated` for a new worktree stack. Discover API ports from Aspire rather than assuming port 5000.
- API client generation requires a running API. From `client/`, `pnpm api` runs generation; if the API is on another port or regeneration is unavailable, follow the hand-edit/fallback instructions in `.agents/skills/api/SKILL.md`. Never accept an empty `swagger.json`.
- Integration tests use Testcontainers and require Docker to execute; without Docker they can compile only.

## Quality Gates

Run relevant gates after the changeset; format before linting.

Frontend, from `client/`:

```sh
pnpm format
pnpm check
pnpm lint
pnpm test
```

Backend, from the repository root:

```sh
dotnet build
csharpier check .
dotnet test
```

Use `csharpier format .` to fix backend formatting. Frontend E2E requires the Aspire stack; start it with `aspire start`, run `pnpm test:e2e` from `client/`, then stop it with `aspire stop`.

Focused backend integration test:

```sh
dotnet test Club.Tests/IntegrationTests/IntegrationTests.csproj -- --filter-class <FullyQualifiedClassName>
```

## Conventions and Domain Facts

- Frontend style: tabs, double quotes, ES5 trailing commas; backend: 4 spaces. Line length is 160 characters. Filenames use kebab-case; types/components use PascalCase.
- Frontend tests: colocated `ComponentName.svelte.test.ts`; E2E: `client/e2e/feature-name.spec.ts`. Backend tests use xUnit and arrange/act/assert.
- Shared API types come from `client/src/lib/api/generated/`. Browser data access uses `$lib/api/remote/*.remote.ts`, backed by server transports in `$lib/server/api/generated/`; authenticated proxying lives in `$lib/server/api/client.ts`. See `client/orval.config.ts` and `tools/gen-remote.mjs` for generation.
- For cached frontend query state, use `@tanstack/svelte-query` (`createQuery` / `createMutation`); wrap remote commands as `mutationFn: (variables) => command(variables)` so TanStack context isn't forwarded. Forms use `createAppForm` in `$lib/components/Form`.
- `@kayord/ui` `Alert.Root` supports `default` and `destructive`; `Button href` accepts `ResolvedPathname`. New dropdowns use its `Select`, not native `<select>`; this version has no `Select.Value`, so render the selected label in `Select.Trigger`. Browser tests click the trigger and an `option` (use `selectOptions()` only for native selects). Check installed component definitions for other variants.
- Booking summary data is split across `bookingGet` (`BookingDTO`) and `bookingGetPath` (`BookingPathDTO`, `GET /booking/{id}/path`). The former includes user, extras, and slot-contract booking data; facility/outlet names come from the latter, not `BookingDTO`. Reuse `BookingPlayers.svelte`, `BookingExtras.svelte`, `$lib/booking/format.ts` (`formatCurrency`, `formatDate`, `formatTime`, `formatDateTime`), `BookingBreadcrumbs`, and `getBookingPayUrl` where applicable.
- API entities are singular PascalCase; DTOs live in `Club.Api/DTO/`. Use `.NET user-secrets` for local secrets.
- In backend tests, pass `TestContext.Current.CancellationToken` (or the FastEndpoints test context's `app.Context.CancellationToken`) through async database, HTTP, and host operations and test helpers. FastEndpoints.Testing `POSTAsync` / `GETAsync<TEndpoint,TReq,TRes>` return `(HttpResponseMessage, TRes?)`; `PUTAsync<TEndpoint,TReq>` returns `HttpResponseMessage`.

## Useful References

- API endpoint patterns and generation details: `.agents/skills/api/SKILL.md`.
- Aspire workflows: `.agents/skills/aspire/SKILL.md` and its routed sub-skills.
- API docs: `<API URL from aspire describe>/scalar/v1`.
- EF migrations: VS Code tasks or `dotnet ef`.
