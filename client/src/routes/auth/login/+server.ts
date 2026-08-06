import { redirect } from "@sveltejs/kit";
import { PUBLIC_APP_URL } from "$env/static/public";
import { createPkce, getAuthorizationUrl, randomNonce, randomState } from "$lib/server/auth/oidc";
import { setPendingLogin } from "$lib/server/auth/session";

// GET /auth/login?next=/demo[&action=UPDATE_PROFILE]
// `action` is a Keycloak account action (UPDATE_PROFILE, UPDATE_PASSWORD, ...)
const SECURE = PUBLIC_APP_URL.startsWith("https");

export async function GET({ url, cookies }) {
	const next = url.searchParams.get("next") ?? "/";
	const action = url.searchParams.get("action");

	const { verifier, challenge } = createPkce();
	const state = randomState();
	const nonce = randomNonce();

	// Verifier + nonce + state live in a short-lived encrypted cookie, so the
	// callback can validate the response without any server-side store.
	setPendingLogin(cookies, { state, nonce, verifier, next }, SECURE);
	throw redirect(303, await getAuthorizationUrl(state, challenge, { nonce, kcAction: action }));
}
