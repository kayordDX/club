// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/example";
import { ExampleVerifyQueryParams } from "$lib/server/api/schemas/example";

export const example = query(async () => api.example());
export const exampleVerify = query(ExampleVerifyQueryParams, async (params) => api.exampleVerify(params));
