import { redirect } from "@sveltejs/kit";
import { getEndSessionUrl } from "$lib/server/auth/oidc";
import { clearSession, readSession } from "$lib/server/auth/session";

// GET /auth/logout — clears the local session, then ends the SSO session at Keycloak.
// Passing the id_token hint gives a smoother single-logout experience.
export async function GET({ cookies }) {
	let idTokenHint: string | undefined;
	const session = readSession(cookies);
	if (session) idTokenHint = session.tokens.idToken;

	clearSession(cookies);
	throw redirect(303, await getEndSessionUrl(idTokenHint));
}
