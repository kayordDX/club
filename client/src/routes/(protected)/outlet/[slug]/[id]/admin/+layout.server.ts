import type { LayoutServerLoad } from "./$types";
import { getFacilityRoles } from "$lib/server/auth/roles";

// The admin layout resets to the root layout (@), so it does not inherit the
// [id] layout's roles. Fetch them here for RoleCheck.
export const load: LayoutServerLoad = async ({ cookies, locals, params }) => {
	let roles: string[] = [];
	if (locals.user && params.id) {
		roles = await getFacilityRoles(cookies, Number(params.id));
	}
	return { user: locals.user, roles };
};
