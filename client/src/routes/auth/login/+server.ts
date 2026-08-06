import { redirect } from "@sveltejs/kit";
import { createPkce, getAuthorizationUrl } from "$lib/server/auth/oidc";
import { createPendingLogin } from "$lib/server/auth/session";

// GET /auth/login?next=/demo[&action=UPDATE_PROFILE]
// `action` is a Keycloak account action (UPDATE_PROFILE, UPDATE_PASSWORD, ...)
export async function GET({ url }) {
	const next = url.searchParams.get("next") ?? "/";
	const action = url.searchParams.get("action");
	const { verifier, challenge } = createPkce();
	const state = createPendingLogin(verifier, next);
	throw redirect(303, await getAuthorizationUrl(state, challenge, action));
}
