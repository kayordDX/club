import { redirect } from "@sveltejs/kit";
import { PUBLIC_API_URL } from "$env/static/public";
import { exchangeCode, getUserinfo, profileFromClaims, SESSION_COOKIE } from "$lib/server/auth/oidc";
import { consumePendingLogin, createSession } from "$lib/server/auth/session";

// GET /auth/callback?code=...&state=...
export async function GET({ url, cookies }) {
	const code = url.searchParams.get("code");
	const state = url.searchParams.get("state");
	const errParam = url.searchParams.get("error");

	if (errParam) {
		throw redirect(303, `/login?error=${encodeURIComponent(errParam)}`);
	}
	if (!code || !state) {
		throw redirect(303, "/login?error=missing_params");
	}

	// State lookup + openid-client's own state/iss validation both guard the code.
	const pending = consumePendingLogin(state);
	if (!pending) {
		throw redirect(303, "/login?error=state_mismatch");
	}

	let result;
	try {
		result = await exchangeCode(url, {
			expectedState: state,
			pkceCodeVerifier: pending.verifier,
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

	const session = createSession(profile, tokens);

	// Provision the user in the API's user table (booking.user_id has an FK to it).
	// Runs server-side at login so the user row exists before any booking write.
	await fetch(`${PUBLIC_API_URL}/account/sync`, {
		method: "POST",
		headers: {
			authorization: `Bearer ${tokens.accessToken}`,
			"content-type": "application/json",
		},
		body: JSON.stringify({ force: false }),
	}).catch((e) => console.warn("account/sync failed at login", e));

	cookies.set(SESSION_COOKIE, session.id, {
		path: "/",
		httpOnly: true,
		sameSite: "lax",
		// secure: true, // TODO: enable in production (HTTPS)
	});

	throw redirect(303, pending.next || "/");
}
