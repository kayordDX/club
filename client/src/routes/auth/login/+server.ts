import { redirect } from "@sveltejs/kit";
import { APP_URL } from "$app/env/private";
import { createPkce, getAuthorizationUrl, randomNonce, randomState } from "$lib/server/auth/oidc";
import { setPendingLogin } from "$lib/server/auth/session";

const SECURE = APP_URL.startsWith("https");

export async function GET({ url, cookies }) {
	const next = url.searchParams.get("next") ?? "/";
	const action = url.searchParams.get("action");

	const { verifier, challenge } = createPkce();
	const state = randomState();
	const nonce = randomNonce();

	setPendingLogin(cookies, { state, nonce, verifier, next }, SECURE);
	throw redirect(303, await getAuthorizationUrl(state, challenge, { nonce, kcAction: action }));
}
