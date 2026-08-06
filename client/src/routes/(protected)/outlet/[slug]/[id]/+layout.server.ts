import type { LayoutServerLoad } from "./$types";
import { accountRole } from "$lib/server/api/generated/account";

// Provides the signed-in user's roles for the current facility to all layouts
// under [id] (including the admin layout, which resets the component chain).
// Roles are consumed by RoleCheck via context.
export const load: LayoutServerLoad = async ({ locals, params }) => {
	let roles: string[] = [];
	if (locals.user && params.id) {
		try {
			const result = await accountRole(Number(params.id));
			roles = result.map((r) => r.normalizedName).filter((x): x is string => !!x);
		} catch {
			// Unauthenticated or API error → no roles (RoleCheck denies access).
		}
	}
	return { user: locals.user, roles };
};
