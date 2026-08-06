// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/extra";

export const extraGetFacility = query(z.number().int(), async (facilityId) => api.extraGetFacility(facilityId));
