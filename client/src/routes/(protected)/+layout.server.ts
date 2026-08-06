import type { LayoutServerLoad } from "./$types";
import { redirect } from "@sveltejs/kit";

// Protect the whole (protected) area using the server session.
export const load: LayoutServerLoad = ({ locals, url }) => {
	if (!locals.user) {
		throw redirect(303, `/auth/login?next=${encodeURIComponent(url.pathname + url.search)}`);
	}
	return { user: locals.user };
};
