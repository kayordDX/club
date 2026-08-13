import type { LayoutServerLoad } from "./$types";
import { getFacilityRoles } from "$lib/server/auth/roles";

// The public facility page renders role-gated actions (e.g. the Admin button),
// so publish the signed-in user's roles for this facility into context.
export const load: LayoutServerLoad = async ({ cookies, locals, params }) => {
	let roles: string[] = [];
	if (locals.user && params.id) {
		roles = await getFacilityRoles(cookies, Number(params.id));
	}
	return { user: locals.user, roles };
};
