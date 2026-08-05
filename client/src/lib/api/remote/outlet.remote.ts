// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/outlet";

export const outletGetBasic = query(z.string(), async (slug) => api.outletGetBasic(slug));
export const outletGetAll = query(async () => api.outletGetAll());
export const outletGet = query(z.string(), async (slug) => api.outletGet(slug));
export const outletAdminGet = query(z.string(), async (slug) => api.outletAdminGet(slug));
