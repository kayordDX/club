# SvelteKit Remote Functions with a .NET Backend — Analysis & Options

> Status: analysis / decision document
> Scope: how to adopt SvelteKit's experimental **remote functions** while keeping .NET (FastEndpoints) as the backend and keeping API code **generated from `swagger.json`**.

---

## TL;DR

1. **SvelteKit remote functions (`$app/server`, `.remote.ts`) are a server-side RPC layer.** They are genuinely excellent for mixing SSR + client code with type-safety, dedup, caching, single-flight mutations, live queries and progressive-enhancement forms.

2. **They require a running SvelteKit Node/serverless server.** Your app today is a **static SPA** (`adapter-static` + `fallback: index.html`, `ssr = false`, deployed via nginx). Remote functions **cannot run** in that setup. Adopting them means switching to `adapter-node` (or a serverless adapter) and **enabling SSR** — a real architecture change, not a config flip.

3. **There is no tool today that generates `.remote.ts` files from OpenAPI.** Remote functions are a *hand-authored server layer*, not a client. The best "generated from .NET" path is: **keep generating types + typed server-side fetch from `swagger.json`**, then wrap those generated helpers in thin, hand-written (or template-generated) `query`/`command`/`form` remote functions.

4. **The biggest non-obvious cost is auth.** Today tokens live client-side (`oidc-client-ts` in `auth.svelte.ts`) and are attached as `Bearer` headers by the orval mutator. With remote functions the **SvelteKit server** must hold/refresh the token to call .NET — i.e. move to **cookie-session + server-side OIDC**. (This is also a *security upgrade*: tokens leave `localStorage`.)

5. **Recommended path: a phased hybrid.** Don't rip out orval/tanstack-query. (a) Keep it for client-side mutations/queries. (b) Add `adapter-node`, server-side auth, and `experimental.remoteFunctions`. (c) Use remote functions **selectively** for SSR-critical public pages (outlets, facilities, booking summaries — the SEO/perf-sensitive surfaces). (d) Generate the types + server-side fetch layer from swagger; hand-author the `query` wrappers (they're ~3 lines each). Optionally build a small swagger→`.remote.ts` template once the pattern stabilises.

---

## 1. What you have today

| Concern | Current state | File / evidence |
|---|---|---|
| Rendering | **Static SPA** — no SSR | `src/routes/+layout.ts` → `export const ssr = false; prerender = false;` |
| Adapter | `@sveltejs/adapter-static` with `fallback: "index.html"` | `svelte.config.js` |
| Deploy | nginx serving `build/` (static files only) | `Dockerfile`, `nginx.conf` |
| API client | **Orval** generates `@tanstack/svelte-query` hooks from `swagger.json` | `orval.config.ts`, `src/lib/api/generated/` |
| Fetch layer | Custom `customInstance` attaches `Bearer` token, maps errors | `src/lib/api/mutator/customInstance.svelte.ts` |
| Auth | **Client-side OIDC** (Keycloak) via `oidc-client-ts`; token in a `$state` store | `src/lib/stores/auth.svelte.ts` |
| Backend | .NET 10 FastEndpoints, separate process, emits OpenAPI | `Club.Api/`, `swagger.json` |
| Orchestration | Aspire runs `web` as a **Vite dev** app + `api` as a project | `Club.AppHost/AppHost.cs` |

Key takeaway: this is SvelteKit's **"separate backend / SPA"** project type. There is currently **no SvelteKit server process in production** — only static assets + the .NET API.

---

## 2. What SvelteKit "remote functions" actually are

Source: SvelteKit docs (`kit/remote-functions`, `$app/server`). Available since **SvelteKit 2.27** (`query`/`command`/`form`/`prerender`) — your `@sveltejs/kit` is `^2.70`, so the feature is available, but it is **experimental** and must be enabled:

```js
// svelte.config.js
const config = {
  kit: { /* ... */ },
  experimental: { remoteFunctions: true }   // <-- required
};
```

Four primitives, all imported from `$app/server` and exported from `*.remote.ts`/`*.remote.js` files:

- **`query([schema,] fn)`** — server-side read. On the client you `await getData()` (or use it in a template) and SvelteKit dedupes + caches by serialised argument. `query.batch` solves N+1; `query.live` streams (SSE-like).
- **`command([schema,] fn)`** — server-side write, callable from event handlers (`await addLike(id)`).
- **`form(schema, fn)`** — progressive-enhancement `<form>` with built-in field validation (`fields.x.as('text')`, `issues()`), server-side `invalid(...)` for programmatic errors, and **single-flight mutations** (auto-refresh affected queries in the same round-trip).
- **`prerender(fn)`** — build-time data, cached in the browser `Cache` API. (This one *can* be used with `adapter-static`, because it runs at build time — see Option D.)

What you get that tanstack-query doesn't give you for free:
- **SSR data hydration** (server renders with data, client reuses it — no client waterfall).
- **Request-scoped dedup** on the server; **shared instance** dedup on the client.
- **Single-flight mutations** + **server-driven / client-requested refreshes** via `requested(...)`.
- **Type-safe RPC end-to-end** with **Standard Schema** (Zod/Valibot) input validation and a `handleValidationError` hook.
- `await`-in-template data loading (pairs with experimental `async` Svelte).

Hard constraints:
- **Must run on a SvelteKit server** (`adapter-node` / serverless). `adapter-static` cannot serve the RPC endpoints.
- Functions are **written by you** in `.remote.ts`. They are **not** derived from OpenAPI.
- `getRequestEvent()` gives cookies/request context — this is how the server attaches credentials to the .NET call.

---

## 3. The core question: "Can we generate remote functions from .NET?"

Short answer: **Not directly — there is no OpenAPI→`.remote.ts` generator.** Here is the landscape, honestly.

### 3a. What generators produce today

| Tool | Output | Produces SvelteKit remote functions? |
|---|---|---|
| **Orval** (your current) | `@tanstack/svelte-query` hooks + types | ❌ No. Clients only: `fetch`, `axios`, `react-query`, `svelte-query`, `vue-query`, `angular`, `zod`. |
| Orval **custom mutator** | A pluggable fetch implementation | ❌ Lets you customise *how* a fetch happens, not *what kind* of construct is emitted. |
| Orval **custom template (`override.templating`)** | Arbitrary files via Handlebars | ⚠️ Technically possible to emit `.remote.ts`, but orval's templating is designed for mutators/headers, not whole new constructs. High effort, fragile. |
| **Microsoft Kiota** | Typed TS/JS client (or C#, etc.) | ❌ Emits a *client* (`fetch`-based), not remote functions. Could be the *transport* a remote function calls, though. |
| **NSwag / OpenAPI Generator (TS)** | `fetch`/`axios` clients + types | ❌ Same — clients only. |
| **Refitter** (.NET) | C# Refit interfaces | ❌ C#-side only. |

**Conclusion:** no off-the-shelf tool emits `.remote.ts`. The reason is structural — a remote function is a *server-side* construct with validation, auth, and cache semantics that don't exist in an OpenAPI spec. The spec can only give you **types** and **transport**.

### 3b. What you *can* generate from `swagger.json` (the realistic building blocks)

1. **Types** — you already get these: `src/lib/api/generated/api.schemas.ts`.
2. **Typed server-side fetch helpers** — a generated function per operation that, given inputs, returns `Promise<Output>` using the right path/method/body. This is *exactly* what orval's `fetch` client produces without the svelte-query wrapper, or what a custom mutator/template can produce.

With (1) and (2), a remote function becomes a **thin wrapper**:

```ts
// src/lib/server/api/booking.remote.ts   (.remote.ts runs on server only)
import { query } from '$app/server';
import { getBooking } from '$lib/server/api/generated/booking'; // generated typed fetch
import { int } from '$lib/server/schema';                       // shared Standard Schema

export const bookingGet = query(int(), async (id) => {
  // server context: attach the user's token (see §5) and call .NET
  return getBooking(id, { auth: getRequestEvent().locals.accessToken });
});
```

So **the fetch layer and types stay 100% generated**; the `.remote.ts` wrapper is ~3–5 lines per operation and is where you put auth/validation mapping. This is "generated from .NET" in everything that's worth generating.

### 3c. Fully-generated wrappers (if you want zero hand-writing)

Once the wrapper pattern is stable, write a small **swagger → `.remote.ts` generator** (eta/handlebars templates over the same `swagger.json`). Map:
- `GET` → `query`, `POST/PUT/PATCH/DELETE` → `command` (and optionally `form` for the few form-shaped endpoints).
- Request schema → a **Standard Schema** (Zod), also generatable from the OpenAPI schema.
- Path/query/body params → the function argument shape.

This is a ~1-day project to build and is maintainable because it's *your* template, not a moving third-party abstraction. It lives alongside `pnpm api`.

---

## 4. Architectural implications of enabling remote functions

This is the part that decides whether it's worth it. Enabling remote functions is **not** a frontend-only change.

### 4a. Rendering + adapter
- `adapter-static` (SPA) → **`adapter-node`** (or `adapter-vercel`/`-netlify`/`-cloudflare`).
- `src/routes/+layout.ts`: flip to SSR (`ssr = true`, or remove the line; keep `prerender` per-page). You can keep selected routes as SPA/prerendered — SvelteKit allows **per-page** rendering, so the migration can be incremental.
- **Deploy:** nginx serving static files → **Node server behind a reverse proxy** (node image + nginx, or serverless). `Dockerfile` and `nginx.conf` both change.

### 4b. Aspire orchestration
- In dev, `web` already runs via `AddViteApp` (Vite SSR/dev), so dev is fine.
- In **production** the AppHost/manifest would describe `web` as a **Node server** (adapter-node), not static assets. Depending on how you deploy, the Aspire publish model for the frontend changes.

### 4c. Auth (the big one)
Today: browser ↔ Keycloak (OIDC via `oidc-client-ts`), token stored client-side, attached by `customInstance`.

With a SvelteKit server you have two models:

| Model | How | Tradeoff |
|---|---|---|
| **A. Keep client OIDC, pass token up** | Client holds token, sends it (header/cookie) to the SvelteKit remote endpoint, which forwards it to .NET | Minimal change; but you still store tokens client-side (security neutral vs today). Server becomes a credentialed proxy. |
| **B. Server-side OIDC (recommended)** | SvelteKit does the auth-code+PKCE flow, stores session in an **httpOnly cookie**, server exchanges for token and calls .NET | **Tokens leave the browser.** More secure. Standard BFF pattern. Requires rewriting `auth.svelte.ts` to use SvelteKit sessions/cookies + hooks. |

Either way, `getRequestEvent().cookies`/`locals` becomes the source of truth for the caller's identity inside `.remote.ts`, and the generated server-side fetch attaches it to the .NET call.

### 4d. Mixing SSR and client code
This is the part remote functions make *delightful*:

```svelte
<!-- runs on server during SSR, hydrates on client, dedupes everywhere -->
<script>
  import { bookingGet } from '$lib/server/api/booking.remote';
  let { data: { id } } = $props();      // id from +page.ts load
  const booking = bookingGet(id);        // a query — awaited in template
</script>

<h1>{(await booking).user.name}</h1>
```

- Same import works on server and client. No separate `+page.server.ts` vs client fetch split.
- Mutations: `command`/`form` + `requested(...)` refresh the right `query` instances **in one round-trip** (single-flight) — you write less invalidation glue than with tanstack-query.
- You can still use **tanstack-query** in parallel for genuinely client-only caches; the two coexist.

---

## 5. Options compared

| Option | Effort | Keeps codegen-from-.NET | Gets remote functions | SSR | Notes |
|---|---|---|---|---|---|
| **A. Status quo (orval + tanstack-query, SPA)** | none | ✅ | ❌ | ❌ | What you have. Fine if SEO/first-paint don't matter. |
| **B. Hybrid (recommended)** | medium | ✅ types + server fetch generated; thin hand-authored `.remote.ts` | ✅ | ✅ selective | Keep tanstack-query where it earns its keep; add remote functions for SSR-critical pages. Phased. |
| **C. Full remote-function rewrite** | high | ⚠️ needs a custom swagger→`.remote.ts` generator | ✅ everywhere | ✅ everywhere | Maximal consistency, but you maintain a generator and migrate every page. |
| **D. `prerender` only (low-risk entry)** | low | ✅ | ✅ (prerender subset) | build-time only | `prerender(...)` works with `adapter-static` (runs at build). Great for outlet/facility catalogues that change rarely. No server, no auth move. **Best first step.** |
| **E. `+page.server.ts` load + `+server.ts` endpoints** | medium | ✅ | ❌ (older model) | ✅ | The pre-remote-functions SvelteKit way. Works, but remote functions are strictly nicer (less boilerplate, dedup, single-flight). Skip unless you can't take the experimental flag. |

---

## 6. Recommended way forward (phased)

### Phase 0 — Decide the deploy/auth posture (1 spike)
Confirm you're OK running a Node server in prod (Docker image change, Aspire manifest) and pick **auth model B** (server-side OIDC, httpOnly cookie). This is the real commitment; everything else is mechanical.

### Phase 1 — Low-risk win: `prerender` for catalogues (Option D)
- Enable `experimental.remoteFunctions` (you can do this even with `adapter-static`).
- Author `prerender` remote functions for **outlets**, **facilities**, and any rarely-changing public catalogue. These run at build time, ship to the CDN, and need **no server and no auth change**.
- Keep orval for everything else.

### Phase 2 — Selective SSR for public pages (Option B)
- Swap `adapter-static` → `adapter-node`. Set `ssr = true` on chosen public routes; keep member/admin routes as CSR for now (per-page control).
- Implement server-side OIDC + cookie session in `hooks.server.ts`; populate `event.locals.accessToken`.
- Generate **server-side typed fetch** from swagger (extend your orval config with a second output, or a small custom mutator/template). Keep types shared.
- Hand-author `query` remote functions for the SSR pages (outlet detail, facility, booking summary). ~3 lines each.
- Use `form` for the booking create/update flows to get progressive enhancement + single-flight refresh.

### Phase 3 — Generator + coverage (Option C, optional)
- Once the wrapper pattern is stable, build a swagger→`.remote.ts` template so new endpoints appear automatically.
- Migrate member/admin pages only where dedup/live-queries/single-flight are worth it; otherwise leave on tanstack-query (it's already good).

---

## 7. Concrete: how codegen stays "from .NET"

The `.remote.ts` files are the **only** thing that's hand-written, and they're trivial because everything they need is generated:

```
swagger.json
   │
   ├─(orval)─▶ api.schemas.ts            (types)            ← already exists
   ├─(orval, new output)─▶ server/*.ts   (typed server fetch) ← new, generated
   └─(your template, Phase 3)─▶ *.remote.ts (thin query/command wrappers) ← generated later
```

No endpoint path, HTTP method, or DTO is ever typed by hand. The hand-authored layer only adds: **auth (from `getRequestEvent()`), validation (Standard Schema, generatable), and cache/mutation semantics** — which is exactly the value remote functions add and OpenAPI can't express.

---

## 8. Decision checklist

Answer these to pick the option:

- [ ] Do public pages need **SEO / fast first paint**? → Yes ⇒ SSR is worth it (Option B/C).
- [ ] Are you willing to **run a Node server in prod** (vs nginx static)? → No ⇒ Option D only (`prerender`).
- [ ] Are you willing to **move auth server-side** (cookie session, httpOnly)? → Strongly recommended regardless (security).
- [ ] Do you want **zero hand-written API glue**? → Then budget for the Phase 3 generator (Option C).
- [ ] Is tanstack-query still earning its keep? → Yes for most apps; keep it alongside remote functions.

**Bottom line:** remote functions are a real upgrade and *can* be fed entirely by code generated from your .NET swagger — but adopting them is an **architecture decision** (SSR + Node server + server-side auth), not a build-tooling tweak. Phase it in starting with `prerender` (zero risk), validate the SSR/auth move on a few public pages, and only invest in the generator once the pattern is proven.
