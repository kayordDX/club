// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/slot";
import { SlotAvailableBody, SlotGetAllQueryParams } from "$lib/server/api/schemas/slot";

export const slotGetContracts = query(z.string(), async (id) => api.slotGetContracts(id));
export const slotGetAll = query(SlotGetAllQueryParams, async (params) => api.slotGetAll(params));
export const slotAvailable = command(SlotAvailableBody, async (body) => api.slotAvailable(body));
