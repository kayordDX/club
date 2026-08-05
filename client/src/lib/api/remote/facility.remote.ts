// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/facility";

export const facilityPaymentMethods = query(z.number().int(), async (facilityId) => api.facilityPaymentMethods(facilityId));
export const facilityGet = query(z.number().int(), async (id) => api.facilityGet(id));
