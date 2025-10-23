import { query } from "$app/server";
import { accountMe } from "$lib/api";

export const me = query(async () => {
	const what = await accountMe();
	return what;
});
