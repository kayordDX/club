# Plan — Remote Functions: Best Practices, Performance & Cleanup (branch: `remote`)

> Audit of the tanstack-query → remote-functions migration. Findings are verified against the code; fixes are prioritised. No code changed in this pass.

---

## A. Why it feels slower than tanstack-query (root causes, ranked)

### A1. Full-page skeleton flash on navigation  ← **biggest perceived cost**
The `<svelte:boundary>` lives in the **root** `+layout.svelte` and wraps `{@render children()}`. Protected pages render `<Header/>` *inside* that subtree (`(protected)/+layout.svelte`). When a page with a top-level `await` suspends, the **root** boundary is the nearest handler, so its `pending()` skeleton replaces the **entire viewport — including the Header**. Every navigation to a top-level-await page (booking detail/edit/pay, settings, slot form) therefore looks like a full page reload.

tanstack-query (SPA) showed the page shell instantly and only the data area spun. That difference is the main "slower" feeling.

**Fix — relocate boundaries to wrap only page content, keep chrome mounted:**
```svelte
<!-- (protected)/+layout.svelte  (and (pages), (public)) -->
<Header />
<svelte:boundary>
	{#snippet pending()} <div class="p-8 animate-pulse">…</div> {/snippet}
	{#snippet failed(e, reset)} … {/snippet}
	{@render children?.()}
</svelte:boundary>
```
Keep a minimal root boundary as a last-resort catch-all, or remove it once each section has its own.

### A2. Sequential awaits = request waterfalls
Pages with multiple top-level awaits fetch one-after-another:
- `(protected)/outlet/…/booking/[bookingId]/pay/+page.svelte` — **3 serial reads** (`bookingGet` → `bookingGetPath` → `facilityPaymentMethods`) before anything renders.
- `bookings/[id]`, `bookings/[id]/edit`, `facility`, `Slot` — 2 serial each.

These are independent calls. **Fix — parallelise with `Promise.all`:**
```ts
const [booking, path, paymentMethods] = await Promise.all([
	bookingGet(bookingId), bookingGetPath(bookingId), facilityPaymentMethods(facilityId)
]);
```
(Top-level `await` of `Promise.all` is allowed under `experimental.async`.) This alone should noticeably speed the pay page.

### A3. No cross-navigation client cache
Remote `query` caches **only while the page is mounted**; navigating away releases it, so back/forward re-fetch. tanstack-query cached across navigations (stale-while-revalidate).

**Fixes (pick by surface):**
- **Public catalogues** (`home`, `outlet`, `facility`, `info`) → wrap in `prerender(...)` so the CDN serves them (zero API cost, instant nav). These change rarely.
- **Dynamic pages** → rely on SvelteKit `data-sveltekit-preload-data="hover"` (already set in `app.html`) to mask latency by starting the fetch on link hover; keep `query` dedup for in-flight sharing. Don't reintroduce a query-cache — that's what we left.

### A4. The DataTable pages render empty, then fetch
`bookings` list and `admin/bookings` use `$state` + `$effect` to fetch. `$effect` runs **client-side after first render**, so SSR emits an empty table and only the client fetches — a double paint and no SSR benefit.
**Fix options:** (a) accept it for behind-login tables (fine), or (b) fetch the first page in a `+page.server.ts` `load` using the server transport for SSR, and keep the `$effect` for subsequent interactive pagination/sort. Document the chosen pattern.

### A5. BFF adds a network hop (inherent)
Browser → SvelteKit server → .NET. The SPA called .NET directly. This is the trade-off of the BFF (token stays server-side). Mitigate via A1–A4 (boundaries, parallelism, prerender, preload) rather than removing the hop.

---

## B. Remote-function best-practice patterns to adopt

1. **Boundary placement** — nearest to the suspending content; keep persistent chrome (Header/Sidebar) outside. Use **local** boundaries for lazy/fine-grained loads (already done for `Slot` price popover + `Extras`; do the same everywhere a sub-area loads).
2. **Parallel reads** — `Promise.all` for independent `query`s (A2).
3. **Errors propagate to `failed(error, reset)`** — `customServerInstance` throws `error(status, msg)`; the boundary shows it. Provide a **consistent reset** (re-trigger) and a meaningful message. Add per-page `failed` snippets where a generic one isn't enough.
4. **Single-flight mutations** — convert booking `command`s to `form(schema, …)` and call `bookingGet(id).refresh()` / `bookingGetUser().refresh()` **inside** the handler so the invalidated data returns in the same round-trip (no second fetch). Currently refreshes are fire-and-forget (`void …refresh()`) which causes a follow-up request.
5. **Validate inputs** — path params use inline Zod; bodies use generated `{OpId}Body`. Keep this; don't regress to `'unchecked'`.
6. **Live data** — `query.live` for booking/payment status on the pay page (pending → confirmed/expired auto-updates, no polling).

---

## C. Auth / BFF hardening

1. **401 mid-session shows "Unauthorized"** ← real UX bug.
   `customServerInstance` throws `error(401)` on an expired token. The hooks refresh ~60 s before expiry, but there's a window where a remote call hits a stale token → boundary shows a bare "Unauthorized".
   **Fix:** on 401, attempt **one** refresh+retry (server-side, using the session's refresh token) before throwing; if refresh fails, redirect to `/auth/login`. Centralise in `customServerInstance` (or a wrapper) so every remote call benefits.
2. **Session store is in-memory** (`src/lib/server/auth/session.ts`). Loses sessions on restart, single-instance only. **Move to Redis** (in the Aspire stack), keyed by the `sid` cookie, same API.
3. **`id_token` is not signature-verified** — we trust Keycloak over HTTPS and use `userinfo`. Adopt `openid-client` (or `jose`) to verify the id_token for defence-in-depth.
4. **Cookie `secure` is off** (dev). Enable in production (HTTPS).
5. **Roles fetched on every `[id]` navigation** (`+layout.server.ts` calls `accountRole`). Adds a round-trip per page load. **Cache roles in the session** (per facility, with a TTL) and refresh lazily.
6. **`/account/sync` is called via raw `fetch`** in `auth/callback/+server.ts`. Use the generated server transport (`accountSync`) for consistency/error handling (it already attaches the token from the event — but here there's no session yet, so pass the token explicitly).
7. **Stale comment** in `hooks.server.ts` ("existing client-side-auth SPA routes are unaffected") — the SPA auth is gone; update the comment.

---

## D. Dead code & cleanup

### Frontend
- **`src/lib/components/QueryError.svelte`** — no longer imported anywhere (Await/Query deleted). **Delete.**
- **`src/lib/components/check/AuthCheck.svelte`** — now a no-op pass-through still referenced by 5 layouts. **Delete it and drop the `<AuthCheck>` wrappers** (protection is in `+layout.server.ts`).
- **`src/lib/types.ts` → `isValidationError`** — only the deleted `customInstance` used it. **Delete** (keep `getError`, still used by TwoFactor; audit `ValidationError` type too).
- **`src/lib/api/generated/*.ts` (per-tag files)** — ~1,650 lines of **dead `fetch` functions** (only `api.schemas.ts` types are used). Either generate schemas-only, or git-ignore/regenerate-and-strip. At minimum, stop shipping them to the client bundle.
- **`src/routes/demo/*`** — scratch/POC routes. Remove once the pattern is proven, or keep behind a flag.
- **`(public)/(auth)/callback/+page.svelte`** — legacy client-OIDC callback, now just redirects to `/`. The server flow uses `/auth/callback`. **Remove** (and drop the route) unless Keycloak still points at `/callback`.

### Backend (`Club.Api/Features`)
Cross-referencing swagger (37 endpoints) against remote-function usage:
- **`Example` and `Test` features** — `example.remote` and `test.remote` are imported by **0** files. Scaffold/demo endpoints (`GET /example`, `/example/verify`, `/test`, `/test/another`). **Candidates to remove** from `Club.Api/Features/Example` and `…/Test` (confirm no other consumer first).
- **Not consumed by the frontend** (verify whether used by payment-provider callbacks/redirects before touching):
  - `GET /outlet/{slug}/admin` (`outletAdminGet`)
  - `GET /payment/form/{provider}/{transactionId}` (`paymentForm`)
  - `POST /payment/checkout/{provider}` (`paymentCheckout`)
  - `GET|POST /payment/result/{provider}` (`paymentResultGet/Post`) — likely the payment-provider return/webhook; the success/failure pages only read query params and don't call these. **Confirm the intended payment flow** and wire or remove.

---

## E. SSR-safety audit
- `app.html` has `data-sveltekit-preload-data="hover"` ✓.
- Public pages use inline `{#await}` (shell stays) ✓; **top-level-await pages** are the flash source (A1).
- `network.svelte.ts`, `ModeWatcher`, `Toaster` guard browser APIs ✓ — no SSR crashes found in the scan, but run the dev server to be sure (cannot verify runtime here).
- **`prerender` page option** is unset everywhere; set it on stable public pages to get CDN caching + instant nav (A3).

---

## Prioritised checklist

**Performance (do first — addresses "slower"):**
1. Move `<svelte:boundary>` from root into each section layout, wrapping only `{@render children}` (keeps Header mounted). *(A1)*
2. Parallelise the multi-await pages with `Promise.all` (pay page first — 3→1 round-trips). *(A2)*
3. `prerender` the public catalogue pages (home/outlet/facility/info). *(A3)*

**Correctness/auth:**
4. 401 refresh-and-retry (or redirect) in `customServerInstance`. *(C1)*
5. Migrate session store to Redis. *(C2)*
6. Cache facility roles in the session. *(C5)*
7. Verify `id_token` / adopt `openid-client`; enable `secure` cookie in prod. *(C3, C4)*

**Remote-function best practices:**
8. Convert booking mutations to `form(...)` with server-driven single-flight refresh. *(B4)*
9. `query.live` for payment status. *(B6)*
10. Consistent `failed(error, reset)` UX + local boundaries for any sub-area load. *(B1, B3)*

**Cleanup:**
11. Delete `QueryError.svelte`, `AuthCheck.svelte` (+ wrappers), `isValidationError`, legacy `/callback` page. *(D)*
12. Stop shipping the dead generated `fetch` functions (schemas-only generation). *(D)*
13. Backend: confirm + remove `Example`/`Test` features; decide the payment result/form/checkout endpoints. *(D)*
14. Refresh stale comments (`hooks.server.ts`). *(C7)*

**Verification (gates after each batch):** `pnpm check`, `pnpm lint`, `pnpm build`, then `dotnet run` the AppHost + `pnpm dev` and click through public + protected + payment flows.

---

## F. adapter-node production runtime & deployment

The `static` → `adapter-node` switch landed, but the **production runtime story** still needs work. The Docker image is now a self-contained Node SSR server (multi-stage build, ~360 MB), but it is **env-agnostic** — all configuration is injected when the container runs.

### Container runtime env (must be set at deploy time)
The server reads these from `process.env` at startup (they are **not** baked into the image):
- `API_URL`, `APP_URL`, `IDENTITY_URL`, `SESSION_SECRET` — defined/validated in `src/env.ts` (explicit env vars). `SESSION_SECRET` must be a strong, unique value (rotation invalidates all sessions).
- `ORIGIN` — adapter-node's own canonical-URL check (CSRF/host validation). Set to the public URL, e.g. `https://club.example.com`. When behind a TLS-terminating proxy also set `PROTOCOL_HEADER` / `HOST_HEADER` / `XFF_DEPTH` so the server sees the real scheme/host.
- `PORT` (default `3000`), `HOST` (default `0.0.0.0`), plus the timeout/socket knobs (`BODY_SIZE_LIMIT`, `SHUTDOWN_TIMEOUT`, …) as needed.

### Port & ingress changed
- Old nginx image listened on **:80**; the node server listens on **:3000**. Any reverse proxy, load balancer, ingress, or health check pointing at `:80` must move to `:3000`.
- **TLS termination moved out of the container.** nginx is gone, so HTTPS must be terminated by an external proxy (nginx/traefik/Caddy/cloud LB). The `secure` cookie flag is derived from `APP_URL`'s scheme — keep `APP_URL` on `https://` in prod.

### No health endpoint
- adapter-node ships no `/health` (unlike the API, which has `WithHttpHealthCheck("/health")`). Add a lightweight SvelteKit `+server.ts` health route (or use TCP readiness) so orchestrators/k8s can liveness/readiness-check the frontend container.

### Aspire publish model
- The AppHost uses `AddViteApp` (dev server) and does not yet describe the frontend for **production** deployment. The prod `web` resource is now the Node image (`ghcr.io/kayorddx/club`), not static assets — wire it into the publish/deploy manifest (or `AddContainer`/`AddDockerfile`) when standing up prod.

### CI build redundancy
- `.github/workflows/build-client.yml` still runs a standalone `pnpm build` before the Docker build. The Dockerfile is now self-contained (multi-stage), so that step is a redundant (if fast-failing) gate that builds twice. Consider dropping it to roughly halve client CI time; the multi-arch `docker buildx` build compiles internally.

### Optional image-size follow-ups
- Current image ~360 MB (`node:22-slim` ~250 + 13 MB prod `node_modules` + 15 MB `build/`). Options to shrink further: switch the runtime base to a distroless Node image, or set Vite `ssr.noExternal: true` so **all** deps bundle into `build/` and ship **zero** `node_modules` (the only prod deps are pure-JS `openid-client` + `@humanspeak/svelte-markdown`, which bundle cleanly). Verify runtime after either change.

### Secret hygiene
- `client/.env.local` is committed and contains a dev `SESSION_SECRET`. Fine for local dev, but: (a) document non-secret defaults in a `.env.example`, and (b) ensure prod injects a strong unique `SESSION_SECRET` rather than the committed dev value.
