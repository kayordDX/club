// GENERATED from swagger.json by scripts/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "#lib/server/api/generated/facility.js";

export const facilityPaymentMethods = query(z.number().int(), async (facilityId) => api.facilityPaymentMethods(facilityId));
export const facilityGet = query(z.number().int(), async (id) => api.facilityGet(id));
