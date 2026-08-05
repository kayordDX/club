// Remote functions for the Bookings feature.
//
// These run on the SvelteKit server and are RPC-called from the client.
// `getRequestEvent()` (inside customServerInstance) supplies the caller's
// access token, so no token ever reaches the browser.
//
// Zod 4 is a Standard Schema, so it plugs straight into `query(schema, fn)`.
import { query } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/booking";

const positiveInt = z.number().int().positive();

/** GET /booking/{id} — booking detail */
export const bookingGet = query(positiveInt, async (id) => api.bookingGet(id));

/** GET /booking/{id}/path — facility/outlet/slot context for a booking */
export const bookingGetPath = query(positiveInt, async (id) => api.bookingGetPath(id));

/** GET /booking/user — the signed-in user's bookings (first page) */
export const bookingGetUser = query(async () => api.bookingGetUser());
