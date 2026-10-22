// GENERATED from swagger.json by scripts/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "#lib/server/api/generated/example.js";
import { ExampleVerifyQueryParams } from "#lib/server/api/schemas/example.js";

export const example = query(async () => api.example());
export const exampleVerify = query(ExampleVerifyQueryParams, async (params) => api.exampleVerify(params));
