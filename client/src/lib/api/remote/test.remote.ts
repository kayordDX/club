// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/test";
import type { TestParams } from "$lib/server/api/generated/server.schemas";

export const testAnother = query(async () => api.testAnother());
export const test = query(z.custom<TestParams>(), async (params) => api.test(params));
