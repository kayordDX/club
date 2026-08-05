import { redirect } from "@sveltejs/kit";
import { createPkce, getAuthorizationUrl } from "$lib/server/auth/oidc";
import { createPendingLogin } from "$lib/server/auth/session";

// GET /auth/login?next=/demo
export async function GET({ url }) {
	const next = url.searchParams.get("next") ?? "/";
	const { verifier, challenge } = createPkce();
	const state = createPendingLogin(verifier, next);
	throw redirect(303, await getAuthorizationUrl(state, challenge));
}
