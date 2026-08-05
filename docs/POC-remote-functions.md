# Remote Functions + Server-side OIDC — Proof of Concept

> Branch: `remote`
> Proves that SvelteKit **remote functions** can drive SSR + hydration against the existing **.NET (FastEndpoints)** backend, with **server-side OIDC (Keycloak)** and code **generated from `swagger.json`**. Nothing here changes the backend.

## What this demonstrates

- ✅ `adapter-static` (SPA) → **`adapter-node`** (SSR server / BFF).
- ✅ **Experimental remote functions** enabled (`kit.experimental.remoteFunctions`).
- ✅ **Server-side OIDC** (auth-code + PKCE) reusing the existing Keycloak `public-client` — **no secret, no Keycloak client changes**. Tokens live server-side; the browser only holds an opaque session cookie.
- ✅ **Remote functions** (`query`) for the Bookings API, with types generated from `swagger.json`.
- ✅ **SSR + hydration**: booking data is fetched on the server with the session token and serialised into the HTML — view-source to confirm. The client reuses it.
- ✅ Existing client-side-auth SPA routes are **untouched** (root `+layout.ts` keeps `ssr = false`).

## How to run

### 1. Backend + Keycloak up

```sh
dotnet run --project Club.AppHost/Club.AppHost.csproj   # API + Keycloak + Postgres + Redis
```

### 2. One-time Keycloak tweak (redirect URI)

The server-side flow redirects to `http://localhost:5173/auth/callback`. In the Keycloak admin console, ensure `public-client` → **Valid redirect URis** includes:

```
http://localhost:5173/*
```

(Most dev Keycloak setups already allow the wildcard — if so, skip this.)

### 3. Frontend (dev)

```sh
cd client
pnpm install
pnpm dev
```

Open <http://localhost:5173/demo>.

- Not authenticated → redirected to Keycloak (Google) via `/auth/login`.
- On return → `/auth/callback` exchanges the code, sets the `sid` cookie, redirects to `/demo`.
- The booking list renders **server-side**. View page source — the bookings are in the HTML.
- Click a booking → `/demo/[id]` renders detail server-side via two `query` remote functions.
- “Sign out” → `/auth/logout` clears the session and ends the Keycloak SSO session.

### 4. Production build (adapter-node)

```sh
pnpm build
ORIGIN=http://localhost:3000 node build   # then open :3000/demo
```

`ORIGIN` is required by adapter-node to know its public URL.

## File map

| File | Purpose |
|---|---|
| `svelte.config.js` | `adapter-node` + `kit.experimental.remoteFunctions` |
| `src/hooks.server.ts` | Cookie → session → `locals.user` / `locals.accessToken`; silent token refresh |
| `src/lib/server/auth/oidc.ts` | Dependency-light OIDC client (discovery, PKCE, token exchange, refresh, userinfo) |
| `src/lib/server/auth/session.ts` | **POC** in-memory session store (swap for Redis in prod) |
| `src/routes/auth/login/+server.ts` | Start OIDC flow |
| `src/routes/auth/callback/+server.ts` | Exchange code → session cookie |
| `src/routes/auth/logout/+server.ts` | Clear session + Keycloak end-session |
| `src/lib/server/api/client.ts` | Server `customServerInstance` — attaches `locals.accessToken` |
| `src/lib/server/api/generated/booking.ts` | **Generated-style** server typed fetch (mirrors orval output) |
| `src/lib/api/booking.remote.ts` | `query` remote functions (importable by client; body runs on server) |
| `src/routes/demo/+layout.server.ts` | Protect `/demo` via server session |
| `src/routes/demo/+page.svelte` | SSR bookings list via remote function |
| `src/routes/demo/[id]/+page.svelte` | SSR booking detail via two remote functions |

> **Why `booking.remote.ts` is in `$lib/api/`, not `$lib/server/`** — `$lib/server/*` is blocked from browser imports. A `.remote.ts` module is special: SvelteKit ships a client-side stub and runs the body on the server, so it must live in a normal location (it *imports* `$lib/server/*`, which is fine — that code only exists in the server bundle).

## Auth flow

```
browser ──(no token, just `sid` cookie)──▶ SvelteKit (adapter-node)
                                              │
                          hooks.server.ts reads cookie → Redis/memory session
                                              │
                  remote query runs HERE ─────┼──(Bearer accessToken)──▶ .NET API
                                              │
   SSR HTML (data serialised) ◀──────────────┘
browser hydrates (reuses data, no re-fetch, no token)
```

Client interactions (refresh, navigation) call the same remote function → same-origin fetch carries the `sid` cookie → server re-authenticates → calls .NET. **The access token never reaches the browser.**

## POC limitations (what to harden before production)

1. **Session store is in-memory** — single-instance, lost on restart. Move to Redis (already in the stack) keyed by `sid`.
2. **`id_token` signature is not verified** — we trust Keycloak over HTTPS and use `userinfo` for the profile. Swap `oidc.ts` for `openid-client` for full validation.
3. **Cookie `secure` is off** — required for `http://` dev; enable in production (HTTPS).
4. **Server-side fetch is hand-mirrored** — generate it from `swagger.json` (orval second output or a small template) so it can’t drift.
5. **Existing SPA routes still use client-side auth** (`oidc-client-ts`) — this POC leaves them alone. Full migration would move every route to the server session.

## Evaluating viability — what to look for

- **Security win**: open DevTools → Application → Cookies. You’ll see `sid` (httpOnly). No access token anywhere in JS. Search the page source — no tokens.
- **SSR win**: `/demo` page source contains the rendered bookings (SEO-friendly, fast first paint).
- **Hydration**: after load, clicking “View” navigates client-side; the remote function dedupes/caches.
- **Token freshness**: wait or shorten expiry — the silent refresh in `hooks.server.ts` keeps calls working without client involvement.
