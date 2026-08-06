# Remote Functions + OIDC: concrete examples

Companion to `remote-functions-analysis.md`. Covers the auth model, hydration behaviour, and a full bookings-API example using the real shape of your `swagger.json` / orval output.

Your stack facts used here:
- **Zod 4** (`^4.4.3`) is already a dependency → it is **Standard Schema** compatible, so it plugs straight into `query(schema, fn)`.
- **Redis** is already in the Aspire stack (`cache`) → ideal server-side session store.
- Existing bookings operations (from `src/lib/api/generated/booking.ts`): `GET /booking/{id}`, `GET /booking/{id}/path`, `GET /booking/user`, `POST /booking`, `PUT /booking/{id}`, `PUT /booking/status`.

---

## 1. The auth model: SvelteKit server becomes the OIDC client

### Who holds what

| | Today (SPA) | With remote functions (BFF) |
|---|---|---|
| OIDC client | **Browser** (`oidc-client-ts`) | **SvelteKit server** (`openid-client` / `arctic`) |
| Access/refresh token | `localStorage` (readable by JS) | **Redis session**, referenced by an opaque cookie ID |
| Browser credential | access token in memory/localStorage | **httpOnly + Secure + SameSite cookie** (session id only) |
| `Authorization: Bearer` to .NET | attached by `customInstance` in the browser | attached by the **SvelteKit server** when it calls .NET |
| Token visible to client JS | ✅ yes | ❌ **never** |

### The flow

1. **First visit / expired session** — `hooks.server.ts` sees no (or expired) session cookie → for protected routes, `redirect(303, '/auth/login')`.
2. **`/auth/login`** is a `+server.ts` (or remote `command`) that runs the OIDC **authorization-code + PKCE** flow *on the server* and redirects to Keycloak.
3. **Keycloak callback** (`/auth/callback`) — server exchanges the code for tokens, stores them in **Redis** under a random `sessionId`, sets `Set-Cookie: sid=<random>; HttpOnly; Secure; SameSite=Lax`.
4. **Every request** — `hooks.server.ts` reads `sid` cookie → loads session from Redis → refreshes the access token if expired (server-side refresh) → sets `event.locals = { user, accessToken }`.
5. **Remote functions** call `getRequestEvent()` → read `locals.accessToken` → attach to the .NET fetch.

### `hooks.server.ts` (sketch)

```ts
// src/hooks.server.ts
import { redirect, type Handle } from '@sveltejs/kit';
import { redis } from '$lib/server/redis';
import { refreshAccessToken, type Session } from '$lib/server/auth/oidc';
import { PUBLIC_API_URL } from '$env/static/public';

export const handle: Handle = async ({ event, resolve }) => {
  const sid = event.cookies.get('sid');
  if (sid) {
    const session = await redis.get<Session>(`session:${sid}`);
    if (session) {
      const accessToken = await refreshAccessTokenIfNeeded(session); // rotates via refresh_token
      event.locals.user = session.user;          // { sub, name, email, ... } — NO token
      event.locals.accessToken = accessToken;     // used only server-side
    }
  }

  // Optional: protect (protected) routes
  if (event.route.id?.startsWith('/(protected)') && !event.locals.user) {
    throw redirect(303, '/auth/login?next=' + encodeURIComponent(event.url.pathname));
  }

  return resolve(event);
};
```

> Keycloak account actions you already model (`UPDATE_PASSWORD`, `CONFIGURE_TOTP`, `VERIFY_EMAIL`, … in `auth.svelte.ts` → `KeycloakAction`) become **server redirects to Keycloak's account console** with a `returnUrl` back to the app. No change in *what* Keycloak does — only *who* initiates it (server instead of browser library).

---

## 2. Hydration: do pages still work on the client? — Yes, seamlessly

This is the part remote functions make automatic. Two phases:

**During SSR** (server):
- You call `const b = bookingGet(id)` and `await` it in the component.
- It executes **on the server**, using `getRequestEvent().locals.accessToken` to call .NET.
- SvelteKit **serialises the result** into the HTML payload.

**During hydration** (client):
- The component re-runs, calls `bookingGet(id)` again — but SvelteKit recognises the serialised result and **reuses it**. **No network call, no token needed on the client.**
- Subsequent client interactions (mutation, navigation, `refresh()`) hit the SvelteKit remote endpoint via a normal `fetch`. Because the endpoint is **same-origin**, the browser **automatically sends the `sid` cookie**. The server re-authenticates from Redis and calls .NET. The token never reaches the browser.

So: **hydration is transparent**, and the client needs **zero** knowledge of OIDC, tokens, or Keycloak. The `oidc-client-ts` dependency and the `auth.svelte.ts` token store go away entirely.

> Contrast with today: `customInstance` reads `auth.accessToken` on the client and attaches it. With the BFF, that exact responsibility moves to the server — but the component code stays the same shape.

---

## 3. Bookings API as remote functions

### 3a. Generated server-side typed fetch (from `swagger.json`)

New orval output (or a small generator) that emits **plain server fetch** (no svelte-query). Types come from the existing `api.schemas.ts`.

```ts
// src/lib/server/api/generated/booking.ts   — GENERATED, do not edit
import { customServerInstance } from '../client';
import type { BookingDTO, BookingPathDTO, BookingCreateRequest, BookingCreateResponse,
              BookingUpdateRequest } from '$lib/api/generated/api.schemas';

export const bookingGet = (id: number) =>
  customServerInstance<BookingDTO>(`/booking/${id}`, { method: 'GET' });

export const bookingGetPath = (id: number) =>
  customServerInstance<BookingPathDTO>(`/booking/${id}/path`, { method: 'GET' });

export const bookingCreate = (body: BookingCreateRequest) =>
  customServerInstance<BookingCreateResponse>(`/booking`, {
    method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body)
  });

export const bookingUpdate = (id: number, body: BookingUpdateRequest) =>
  customServerInstance<void>(`/booking/${id}`, {
    method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body)
  });
// ...
```

```ts
// src/lib/server/api/client.ts — the server equivalent of your current customInstance
import { PUBLIC_API_URL } from '$env/static/public';
import { getRequestEvent } from '$app/server';

export async function customServerInstance<T>(path: string, init: RequestInit): Promise<T> {
  const { accessToken } = getRequestEvent().locals;          // injected by hooks.server.ts
  const res = await fetch(`${PUBLIC_API_URL}${path}`, {
    ...init,
    headers: { ...(init.headers ?? {}), Authorization: `Bearer ${accessToken}` }
  });
  if (!res.ok) throw new Error(`${res.status} ${path}`);
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}
```

> Note: the `.remote.ts` files and anything under `src/lib/server/**` are **server-only** modules — SvelteKit enforces this; they're stripped from the client bundle.

### 3b. The remote functions (`.remote.ts`) — thin, hand-authored, validated

```ts
// src/lib/server/api/booking.remote.ts
import { query, command, form, getRequestEvent } from '$app/server';
import { z } from 'zod';
import * as api from './generated/booking';                  // generated, above
import { bookingGet, bookingGetUser } from './booking.remote'; // self-ref for single-flight

// --- Reads (query) ---

export const bookingGet = query(z.number().int(), async (id) => {
  // accessToken is attached inside customServerInstance via getRequestEvent()
  return api.bookingGet(id);
});

export const bookingGetPath = query(z.number().int(), async (id) => api.bookingGetPath(id));

// Params validated too — generate these Zod schemas from OpenAPI with orval `client: 'zod'`
const UserParams = z.object({
  pageNumber: z.number().int().optional(),
  pageSize: z.number().int().optional(),
  status: z.enum(['Pending', 'Confirmed', 'Cancelled']).optional(),
});
export const bookingGetUser = query(UserParams, async (params) => api.bookingGetUser(params));

// --- Writes (command / form) ---

const CreateBody = z.object({
  facilityId: z.number().int(),
  slotContractBookings: z.array(z.object({
    slotContractId: z.number().int(),
    players: z.array(z.string()),
  })),
  extraBookings: z.array(z.object({ extraId: z.number().int(), qty: z.number().int() })).optional(),
});

// form() gives progressive enhancement + single-flight refresh
export const bookingCreate = form(CreateBody, async (data) => {
  const created = await api.bookingCreate(data);
  // Server-driven refresh: invalidate the user's bookings list in the SAME response
  void bookingGetUser({}).refresh();
  return created;                                            // available as bookingCreate.result
});

const UpdateBody = z.object({ id: z.number().int() }).passthrough(); // or a precise schema
export const bookingUpdate = command(z.object({ id: z.number().int(), data: UpdateBody }), async ({ id, data }) => {
  await api.bookingUpdate(id, data);
  // Single-flight: the server pushes the updated booking to the client cache, no second round-trip
  await bookingGet(id).refresh();
});
```

### 3c. Using it in a page — SSR + hydration, same code server & client

```svelte
<!-- src/routes/(protected)/bookings/[id]/+page.svelte -->
<script lang="ts">
  import { bookingGet, bookingGetPath } from '$lib/server/api/booking.remote';
  let { data } = $props();                  // data.id from +page.ts load (just passes the param)

  // A query: awaited in template → runs on server during SSR, reuses serialised value on hydrate
  const booking   = bookingGet(data.id);
  const bookingPath = bookingGetPath(data.id);
</script>

<h1>Booking #{(await booking).id}</h1>
<p>Facility: {(await bookingPath).facilityName}</p>
<p>Player: {(await booking).user.name}</p>

<button onclick={async () => {
  // same function, now called on the client → same-origin fetch carries the sid cookie automatically
  await bookingGet(data.id).refresh();
}}>Refresh</button>
```

### 3d. Create booking as a progressive-enhancement form (single-flight)

```svelte
<!-- src/routes/(protected)/outlet/[slug]/[id]/booking/+page.svelte -->
<script lang="ts">
  import { bookingCreate } from '$lib/server/api/booking.remote';
  const { fields } = bookingCreate;
</script>

<!-- {...bookingCreate} spreads method/action so it works WITHOUT javascript -->
<form {...bookingCreate.enhance(async (f) => {
       if (await f.submit()) f.element.reset();
     })}>
  <input {...fields.facilityId.as('number')} />
  {#each fields.slotContractBookings[0].players as _, i}
    <input {...fields.slotContractBookings[0].players[i].as('text')} placeholder="player" />
  {/each}
  <button>Create booking</button>
</form>

{#if bookingCreate.result}
  <p>Created booking #{bookingCreate.result.id}</p>
{/if}

<!-- the bookings list refreshes automatically in the same round-trip (server-driven) -->
```

### 3e. Where your existing code maps

| Today (orval + tanstack-query + oidc-client-ts) | With remote functions |
|---|---|
| `customInstance.svelte.ts` (client, attaches token) | `src/lib/server/api/client.ts` (server, attaches token from `locals`) |
| `createBookingGet(() => id)` | `bookingGet(id)` awaited in template |
| `createBookingCreate()` mutation + manual `queryClient.invalidateQueries` | `form(...)` + `void bookingGetUser().refresh()` (single-flight) |
| `auth.svelte.ts` token store | `event.locals` (server) + a `currentUser` query that returns **non-secret** profile |
| `oidc-client-ts` | `openid-client` / `arctic` in `src/lib/server/auth/` |
| `getError`/`isValidationError` | throw in the server fetch → SvelteKit `handleError` hook |

---

## 4. Synergy: generate the Zod schemas too

orval has a `zod` client (`client: 'zod'`). Add a second output to `orval.config.ts`:

```ts
schemas: {
  input: './swagger.json',
  output: { mode: 'tags', target: 'schemas.ts', client: 'zod', ... }
}
```

→ you get `bookingCreateBodySchema` etc. generated from .NET, which you feed directly into `form(schema, ...)` / `query(schema, ...)`. **Types, transport, *and* validation all generated from `swagger.json`.** The `.remote.ts` layer then contains only the value-add glue (auth wiring, cache/mutation semantics).
