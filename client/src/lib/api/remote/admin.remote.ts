// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/admin";
import type { AdminBookingGetAllParams, AdminBookingUpdateRequest, AdminBookingUpdateStatusRequest } from "$lib/server/api/generated/server.schemas";

export const adminBookingUpdateStatus = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), body: z.custom<AdminBookingUpdateStatusRequest>() }),
	async ({ facilityId, id, body }) => api.adminBookingUpdateStatus(facilityId, id, body)
);
export const adminBookingUpdate = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), body: z.custom<AdminBookingUpdateRequest>() }),
	async ({ facilityId, id, body }) => api.adminBookingUpdate(facilityId, id, body)
);
export const adminBookingGet = query(z.object({ facilityId: z.number().int(), id: z.number().int() }), async ({ facilityId, id }) =>
	api.adminBookingGet(facilityId, id)
);
export const adminBookingGetAll = query(
	z.object({ facilityId: z.number().int(), params: z.custom<AdminBookingGetAllParams>().optional() }),
	async ({ facilityId, params }) => api.adminBookingGetAll(facilityId, params)
);
