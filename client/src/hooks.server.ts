import type { Handle } from "@sveltejs/kit";
import { getSession, deleteSession } from "$lib/server/auth/session";
import { refreshTokens, SESSION_COOKIE } from "$lib/server/auth/oidc";

/**
 * Resolves the cookie session into `event.locals` for every request.
 *
 * Auth *enforcement* is done per-route in `+layout.server.ts` load functions,
 * not here — so the existing client-side-auth SPA routes are unaffected.
 */
export const handle: Handle = async ({ event, resolve }) => {
	const sid = event.cookies.get(SESSION_COOKIE);

	if (sid) {
		const session = getSession(sid);
		if (session) {
			// Refresh ~60s before the access token expires.
			const needsRefresh = session.tokens.expiresAt - Date.now() < 60_000 && !!session.tokens.refreshToken;

			if (needsRefresh) {
				try {
					session.tokens = await refreshTokens(session.tokens.refreshToken!);
				} catch {
					// Refresh failed — drop the session; user must re-authenticate.
					deleteSession(sid);
					event.cookies.delete(SESSION_COOKIE, { path: "/" });
					return resolve(event);
				}
			}

			event.locals.user = session.user;
			event.locals.accessToken = session.tokens.accessToken;
		} else {
			// Stale cookie — clean it up.
			event.cookies.delete(SESSION_COOKIE, { path: "/" });
		}
	}

	return resolve(event);
};
