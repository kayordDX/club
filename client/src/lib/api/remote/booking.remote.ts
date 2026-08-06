// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/booking";
import { BookingCreateBody, BookingGetUserQueryParams, BookingUpdateBody, BookingUpdateStatusBody } from "$lib/server/api/schemas/booking";

export const bookingUpdateStatus = command(BookingUpdateStatusBody, async (body) => api.bookingUpdateStatus(body));
export const bookingUpdate = command(z.object({ id: z.number().int(), body: BookingUpdateBody }), async ({ id, body }) => api.bookingUpdate(id, body));
export const bookingGet = query(z.number().int(), async (id) => api.bookingGet(id));
export const bookingGetUser = query(BookingGetUserQueryParams.optional(), async (params) => api.bookingGetUser(params));
export const bookingGetPath = query(z.number().int(), async (id) => api.bookingGetPath(id));
export const bookingCreate = command(BookingCreateBody, async (body) => api.bookingCreate(body));
