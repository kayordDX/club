// Server-side equivalent of the client `customInstance.svelte.ts`.
// Attaches the caller's access token (from the session, never sent to the browser)
// to every call to the .NET API.
import { getRequestEvent } from "$app/server";
import { error } from "@sveltejs/kit";
import { PUBLIC_API_URL } from "$env/static/public";

export async function customServerInstance<T>(path: string, init: RequestInit = {}): Promise<T> {
	const accessToken = getRequestEvent().locals.accessToken;
	if (!accessToken) {
		throw error(401, "Not authenticated");
	}

	const res = await fetch(`${PUBLIC_API_URL}${path}`, {
		...init,
		headers: {
			...(init.headers ?? {}),
			authorization: `Bearer ${accessToken}`,
		},
	});

	if (res.status === 401) throw error(401, "Unauthorized");
	if (res.status === 404) throw error(404, "Not found");
	if (!res.ok) {
		throw error(res.status, `API request failed: ${res.status}`);
	}
	if (res.status === 204) return undefined as T;
	return (await res.json()) as T;
}
