// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/wallet";
import { WalletTransferBody } from "$lib/server/api/schemas/wallet";

export const walletVouchers = query(async () => api.walletVouchers());
export const walletTransfer = command(z.object({ grantId: z.string(), body: WalletTransferBody }), async ({ grantId, body }) =>
	api.walletTransfer(grantId, body)
);
