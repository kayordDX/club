# Plan — Remote Functions Migration (branch: `remote`)

## Status: what is done

**tanstack-query fully removed**
- All `createX` query/mutation hooks replaced with remote functions (`query`/`command` from `$app/server`).
- Removed from orval: the `api` output is now `client: 'fetch'` (kept only for the shared TS types/enums — the generated fetch functions are dead code).
- `@tanstack/svelte-query` and `oidc-client-ts` removed from `package.json`; `QueryClientProvider`, `customInstance.svelte.ts`, `Query.svelte`, and the `auth.svelte.ts` store deleted.

**Data loading pattern (per request)**
- `<svelte:boundary>` in the root layout provides `pending()` (skeleton) and `failed(error, reset)` for every page.
- Pages load data with `await`:
  - template: `{@const outlet = await outletGet(slug)}` inside a `<svelte:boundary>`
  - script: `const booking = await bookingGet(id)` (top-level await, `experimental.async`)
- Reactive reads: `$derived(remoteFn(arg))` re-await on arg change (home search, slot calendar date).
- `+server.ts` kept to the absolute minimum: `/auth/login`, `/auth/callback`, `/auth/logout` only. The old `/callback` page just redirects home.

**Auth**
- Server-side OIDC (auth-code + PKCE, Keycloak `public-client`); session in memory (POC), token never reaches the browser.
- Profile page uses `useUser()` context; Keycloak account actions via `/auth/login?action=...` (`kc_action`).

## Next steps

### 1. Runtime verification against the full stack (do this first)
Nothing here has been run against Keycloak + API + Postgres + Redis. Start the AppHost, run `pnpm dev`, and click through:
- Public: home → outlet → facility → slot calendar → booking form → payment.
- Protected: bookings list/detail/edit, admin bookings, settings (profile/session/2FA), pay/cancel.
Fix whatever surfaces (error mapping from remote `command`s, boundary placement, `.refresh()` semantics).

### 2. Replace in-memory sessions with Redis
`src/lib/server/auth/session.ts` is a `Map`. Swap for Redis (already in the Aspire stack) keyed by the session cookie; keep the same API surface. Add session expiry + refresh-token rotation.

### 3. Harden OIDC
- Verify `id_token` signatures (swap the manual client in `src/lib/server/auth/oidc.ts` for `openid-client`, or add jose verification).
- Enable `secure` on the session cookie in production.
- Confirm Keycloak `public-client` redirect URIs include `http://localhost:5173/auth/callback` and `http://localhost:5173/*`.

### 4. Decide the interactive-table pattern
`bookings` list and `admin/bookings` use a client-side `$effect` + `$state` fetch (remote function, no SSR) because server-pagination needs reactive params. Options to make them first-class:
- Keep as-is (fine behind login), or
- Move initial page to a `+layout.server.ts` load (fetch first page server-side, hydrate) while keeping the effect for subsequent interactions.

### 5. Use `form` remote functions for the booking flow
`bookingCreate`/`bookingUpdate` are `command`. Convert to `form(schema, ...)` to get progressive enhancement + single-flight invalidation of `bookingGet`/`bookingGetUser` in the same round-trip.

### 6. Live queries where they add value
`query.live` for booking/payment status on the pay page (auto-updating pending → confirmed/expired without polling).

### 7. `prerender` for public catalogues
Outlets/facilities change rarely; wrap in `prerender(...)` so the CDN serves them (zero API load). Requires deciding the deploy shape (adapter-node already set).

### 8. Standardize the two await styles
Pages currently mix `{@const x = await fn()}` (template, inside a boundary) and script top-level `await`. Pick one convention per page type:
- Script-level `await` when the data feeds deriveds/handlers (booking flows).
- Template `{@const}` + boundary for simple reads.
Document it in `AGENTS.md`.

### 9. Clean up orval output
The `api` output's per-tag fetch functions are dead code (kept for `api.schemas.ts`). Check orval for a schemas-only mode; if not, consider generating schemas once and deleting the dead functions.

### 10. Tests
- Unit tests that render components with remote functions must mock `$lib/api/remote/*` (see `(pages)/page.svelte.spec.ts`).
- Add unit tests for the `Await`/boundary pattern and for form validators.
- E2E: extend `client/e2e/` with an SSR smoke test (assert server-rendered content in the HTML).

## Conventions to keep
- Remote functions are the ONLY data layer; no `+page.server.ts`/`+server.ts` for data.
- Validation: path params inline Zod; bodies via generated `{OpId}Body` schemas (orval `zod` output).
- Everything generated: `pnpm api` → orval (3 outputs) + `tools/gen-remote.mjs` + format.
- `<svelte:boundary>` in layouts; local boundaries for lazy/fine-grained loads (popovers, Extras) so they don't blank the page.
