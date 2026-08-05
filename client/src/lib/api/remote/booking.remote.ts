// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/booking";
import type { BookingCreateRequest, BookingUpdateRequest, BookingUpdateStatusRequest } from "$lib/server/api/generated/server.schemas";

export const bookingUpdateStatus = command(z.custom<BookingUpdateStatusRequest>(), async (body) => api.bookingUpdateStatus(body));
export const bookingUpdate = command(z.object({ id: z.number().int(), body: z.custom<BookingUpdateRequest>() }), async ({ id, body }) =>
	api.bookingUpdate(id, body)
);
export const bookingGet = query(z.number().int(), async (id) => api.bookingGet(id));
export const bookingGetUser = query(async () => api.bookingGetUser());
export const bookingGetPath = query(z.number().int(), async (id) => api.bookingGetPath(id));
export const bookingCreate = command(z.custom<BookingCreateRequest>(), async (body) => api.bookingCreate(body));
