// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/slot";
import type { AvailableSlotRequest, SlotGetAllParams } from "$lib/server/api/generated/server.schemas";

export const slotGetContracts = query(z.string(), async (id) => api.slotGetContracts(id));
export const slotGetAll = query(z.custom<SlotGetAllParams>(), async (params) => api.slotGetAll(params));
export const slotAvailable = command(z.custom<AvailableSlotRequest>(), async (body) => api.slotAvailable(body));
