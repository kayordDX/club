// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.
// Validation schemas come from orval's zod client output.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/account";
import { AccountCredentialDisableBody, AccountSessionRevokeBody, AccountSyncBody } from "$lib/server/api/schemas/account";

export const accountSync = command(AccountSyncBody, async (body) => api.accountSync(body));
export const accountSession = query(async () => api.accountSession());
export const accountSessionRevokeAll = command(async () => api.accountSessionRevokeAll());
export const accountSessionRevoke = command(AccountSessionRevokeBody, async (body) => api.accountSessionRevoke(body));
export const accountRole = query(z.number().int(), async (facilityId) => api.accountRole(facilityId));
export const accountCredential = query(async () => api.accountCredential());
export const accountCredentialDisableToken = command(async () => api.accountCredentialDisableToken());
export const accountCredentialDisable = command(AccountCredentialDisableBody, async (body) => api.accountCredentialDisable(body));
