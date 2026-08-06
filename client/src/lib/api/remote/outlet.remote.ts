// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/outlet";
import { OutletGetAllQueryParams } from "$lib/server/api/schemas/outlet";

export const outletGetBasic = query(z.string(), async (slug) => api.outletGetBasic(slug));
export const outletGetAll = query(OutletGetAllQueryParams.optional(), async (params) => api.outletGetAll(params));
export const outletGet = query(z.string(), async (slug) => api.outletGet(slug));
export const outletAdminGet = query(z.string(), async (slug) => api.outletAdminGet(slug));
