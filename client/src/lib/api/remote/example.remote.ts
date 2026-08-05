// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/example";
import type { ExampleVerifyParams } from "$lib/server/api/generated/server.schemas";

export const example = query(async () => api.example());
export const exampleVerify = query(z.custom<ExampleVerifyParams>(), async (params) => api.exampleVerify(params));
