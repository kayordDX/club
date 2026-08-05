import { redirect } from "@sveltejs/kit";
import { getEndSessionUrl, SESSION_COOKIE } from "$lib/server/auth/oidc";
import { deleteSession } from "$lib/server/auth/session";

// GET /auth/logout  — clears local session then ends the SSO session at Keycloak.
export async function GET({ cookies }) {
	const sid = cookies.get(SESSION_COOKIE);
	if (sid) {
		deleteSession(sid);
		cookies.delete(SESSION_COOKIE, { path: "/" });
	}
	throw redirect(303, await getEndSessionUrl());
}
