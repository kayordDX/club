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
	AdminContractAddMemberBody,
	AdminContractCreateBody,
	AdminContractCreateMemberBody,
	AdminContractSearchMemberQueryParams,
	AdminContractUpdateBody,
	AdminContractUpdateMemberBody,
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
export const adminContractGetAll = query(z.number().int(), async (facilityId) => api.adminContractGetAll(facilityId));
export const adminContractGetMembers = query(z.object({ facilityId: z.number().int(), id: z.number().int() }), async ({ facilityId, id }) =>
	api.adminContractGetMembers(facilityId, id)
);
export const adminContractSearchMember = query(
	z.object({ facilityId: z.number().int(), id: z.number().int(), params: AdminContractSearchMemberQueryParams }),
	async ({ facilityId, id, params }) => api.adminContractSearchMember(facilityId, id, params)
);
export const adminContractAddMember = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), body: AdminContractAddMemberBody }),
	async ({ facilityId, id, body }) => api.adminContractAddMember(facilityId, id, body)
);
export const adminContractCreateMember = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), body: AdminContractCreateMemberBody }),
	async ({ facilityId, id, body }) => api.adminContractCreateMember(facilityId, id, body)
);
export const adminContractUpdateMember = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), memberId: z.number().int(), body: AdminContractUpdateMemberBody }),
	async ({ facilityId, id, memberId, body }) => api.adminContractUpdateMember(facilityId, id, memberId, body)
);
export const adminContractRemoveMember = command(
	z.object({ facilityId: z.number().int(), id: z.number().int(), memberId: z.number().int() }),
	async ({ facilityId, id, memberId }) => api.adminContractRemoveMember(facilityId, id, memberId)
);
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
