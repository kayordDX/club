import type { LayoutServerLoad } from "./$types";
import { accountRole } from "$lib/server/api/generated/account";

// The admin layout resets to the root layout (@), so it does not inherit the
// [id] layout's roles. Fetch them here for RoleCheck.
export const load: LayoutServerLoad = async ({ locals, params }) => {
	let roles: string[] = [];
	if (locals.user && params.id) {
		try {
			const result = await accountRole(Number(params.id));
			roles = result.map((r) => r.normalizedName).filter((x): x is string => !!x);
		} catch {
			// ignore — RoleCheck denies access
		}
	}
	return { user: locals.user, roles };
};
