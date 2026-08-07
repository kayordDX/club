import { redirect } from "@sveltejs/kit";
import { API_URL, APP_URL } from "$app/env/private";
import { exchangeCode, getUserinfo, profileFromClaims } from "$lib/server/auth/oidc";
import { consumePendingLogin, toSessionUser, writeSession } from "$lib/server/auth/session";

const SECURE = APP_URL.startsWith("https");

// GET /auth/callback?code=...&state=...
export async function GET({ url, cookies }) {
	const errParam = url.searchParams.get("error");
	if (errParam) {
		throw redirect(303, `/login?error=${encodeURIComponent(errParam)}`);
	}

	// Pending login (state + nonce + PKCE verifier) is read — and consumed — from
	// the encrypted cookie. openid-client additionally validates `state`/`iss`
	// against the callback URL, so a missing/mismatched cookie aborts cleanly.
	const pending = consumePendingLogin(cookies);
	if (!pending) {
		throw redirect(303, "/login?error=state_mismatch");
	}

	const code = url.searchParams.get("code");
	if (!code) {
		throw redirect(303, "/login?error=missing_code");
	}

	let result;
	try {
		result = await exchangeCode(url, {
			expectedState: pending.state,
			pkceCodeVerifier: pending.verifier,
			expectedNonce: pending.nonce,
		});
	} catch (e) {
		console.warn("OIDC code exchange failed", e);
		throw redirect(303, "/login?error=exchange_failed");
	}

	const { tokens } = result;
	const profile = await getUserinfo(tokens.accessToken, result.subject).catch(() =>
		// Keycloak unreachable mid-login: fall back to the (verified) id_token claims.
		profileFromClaims(result.claims)
	);

	// Provision the user in the API's user table (booking.user_id has an FK to it).
	// Runs server-side at login so the row exists before any booking write.
	await fetch(`${API_URL}/account/sync`, {
		method: "POST",
		headers: {
			authorization: `Bearer ${tokens.accessToken}`,
			"content-type": "application/json",
		},
		body: JSON.stringify({ force: false }),
	}).catch((e) => console.warn("account/sync failed at login", e));

	writeSession(cookies, { user: toSessionUser(profile), tokens }, SECURE);
	throw redirect(303, pending.next || "/");
}
