import { getRequestEvent, query } from "$app/server";
import { test as testApi, type User } from "$lib/api";
import { PUBLIC_API_URL } from "$env/static/public";

export const test = query(async () => {
	const { fetch } = getRequestEvent();
	const result = await fetch(`${PUBLIC_API_URL}/test?name=test`);
	if (!result.ok) {
		throw new Error(`Error fetching test data: ${result.status} ${result.statusText}`);
	}
	const data: User[] = await result.json();
	return data;
	// return await testApi({ name: "test" });
});
