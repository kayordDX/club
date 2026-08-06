// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/test";
import { TestQueryParams } from "$lib/server/api/schemas/test";

export const testAnother = query(async () => api.testAnother());
export const test = query(TestQueryParams, async (params) => api.test(params));
