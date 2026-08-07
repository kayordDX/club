import type { Handle } from "@sveltejs/kit";
import { APP_URL } from "$app/env/private";
import { refreshTokens } from "$lib/server/auth/oidc";
import { clearSession, readSession, writeSession } from "$lib/server/auth/session";

// Hot path is intentionally cheap: read + decrypt the session cookie (pure CPU,
// no store round-trip), and only hit Keycloak to refresh when the access token
// is about to expire.
const SECURE = APP_URL.startsWith("https");
const REFRESH_MARGIN_MS = 60_000;

export const handle: Handle = async ({ event, resolve }) => {
	const session = readSession(event.cookies);
	if (session) {
		const stale = session.tokens.expiresAt - Date.now() < REFRESH_MARGIN_MS;
		if (stale && session.tokens.refreshToken) {
			try {
				session.tokens = await refreshTokens(session.tokens.refreshToken);
				writeSession(event.cookies, session, SECURE);
			} catch {
				// Refresh failed — drop the session; user must re-authenticate.
				clearSession(event.cookies);
				return resolve(event);
			}
		}
		event.locals.user = session.user;
		event.locals.accessToken = session.tokens.accessToken;
	}
	return resolve(event);
};
