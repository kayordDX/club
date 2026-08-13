import type { Cookies } from "@sveltejs/kit";
import { APP_URL } from "$app/env/private";
import { accountRole } from "$lib/server/api/generated/account";
import { readSession, writeSession } from "./session";

// Facility roles are stable and change rarely (manager assigns them), so cache
// them per facility in the session and refresh lazily instead of calling
// /account/role on every [id] navigation. All outlet layouts share this cache.
const ROLES_TTL_MS = 5 * 60 * 1000;
/** Cap the cache so the (encrypted, chunked) session cookie stays small. */
const MAX_CACHED_FACILITIES = 20;

const SECURE = APP_URL.startsWith("https");

export async function getFacilityRoles(cookies: Cookies, facilityId: number): Promise<string[]> {
	const session = readSession(cookies);
	if (!session) return [];

	const cached = session.rolesCache?.[facilityId];
	if (cached && cached.expiresAt > Date.now()) return cached.roles;

	let roles: string[] = [];
	try {
		const result = await accountRole(facilityId);
		roles = result.map((r) => r.normalizedName).filter((x): x is string => !!x);
	} catch {
		// Not authenticated or API error → no roles (role-gated UI is hidden).
	}

	// Drop stale entries, then cap the cache by evicting the soonest-expiring one.
	const entries = Object.entries(session.rolesCache ?? {}).filter(([, e]) => e.expiresAt > Date.now());
	if (entries.length >= MAX_CACHED_FACILITIES) {
		entries.sort((a, b) => a[1].expiresAt - b[1].expiresAt);
		entries.shift();
	}
	session.rolesCache = Object.fromEntries([...entries, [String(facilityId), { roles, expiresAt: Date.now() + ROLES_TTL_MS }]]);
	writeSession(cookies, session, SECURE);

	return roles;
}
