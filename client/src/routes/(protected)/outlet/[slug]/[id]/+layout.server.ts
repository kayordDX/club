import type { LayoutServerLoad } from "./$types";
import { getFacilityRoles } from "#lib/server/auth/roles.js";

// Provides the signed-in user's roles for the current facility to all layouts
// under [id] (including the admin layout, which resets the component chain).
// Roles are consumed by RoleCheck via context.
export const load: LayoutServerLoad = async ({ cookies, locals, params }) => {
	let roles: string[] = [];
	if (locals.user && params.id) {
		roles = await getFacilityRoles(cookies, Number(params.id));
	}
	return { user: locals.user, roles };
};
