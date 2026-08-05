import { redirect } from "@sveltejs/kit";
import type { LayoutServerLoad } from "./$types";

// Protect the whole /demo area using the *server* session (cookie).
// Unauthenticated users are sent to the server-side OIDC login flow.
export const load: LayoutServerLoad = ({ locals, url }) => {
	if (!locals.user) {
		throw redirect(303, `/auth/login?next=${encodeURIComponent(url.pathname)}`);
	}
	return { user: locals.user };
};
