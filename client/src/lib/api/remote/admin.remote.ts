// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/admin";
import {
	AdminBookingGetAllQueryParams,
	AdminBookingUpdateBody,
	AdminBookingUpdateStatusBody,
	AdminContractCreateBody,
	AdminContractUpdateBody,
	AdminSlotGetAllQueryParams,
} from "$lib/server/api/schemas/admin";

export const adminBookingUpdateStatus = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), body: AdminBookingUpdateStatusBody }),
	async ({ facilityId, id, body }) => api.adminBookingUpdateStatus(facilityId, id, body)
);
export const adminBookingUpdate = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), body: AdminBookingUpdateBody }),
	async ({ facilityId, id, body }) => api.adminBookingUpdate(facilityId, id, body)
);
export const adminBookingGet = query(z.object({ facilityId: z.number().int(), id: z.number().int() }), async ({ facilityId, id }) =>
	api.adminBookingGet(facilityId, id)
);
export const adminBookingGetAll = query(
	z.object({ facilityId: z.number().int(), params: AdminBookingGetAllQueryParams.optional() }),
	async ({ facilityId, params }) => api.adminBookingGetAll(facilityId, params)
);
export const adminSlotGetAll = query(z.object({ facilityId: z.number().int(), params: AdminSlotGetAllQueryParams }), async ({ facilityId, params }) =>
	api.adminSlotGetAll(facilityId, params)
);
export const adminContractGetAll = query(z.object({ facilityId: z.number().int() }), async ({ facilityId }) => api.adminContractGetAll(facilityId));
export const adminContractGet = query(z.object({ facilityId: z.number().int(), id: z.number().int() }), async ({ facilityId, id }) =>
	api.adminContractGet(facilityId, id)
);
export const adminContractCreate = command(z.object({ facilityId: z.number().int(), body: AdminContractCreateBody }), async ({ facilityId, body }) =>
	api.adminContractCreate(facilityId, body)
);
export const adminContractUpdate = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), body: AdminContractUpdateBody }),
	async ({ facilityId, id, body }) => api.adminContractUpdate(facilityId, id, body)
);
export const adminContractDelete = command(z.object({ facilityId: z.number().int(), id: z.number().int() }), async ({ facilityId, id }) =>
	api.adminContractDelete(facilityId, id)
);
