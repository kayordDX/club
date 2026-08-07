// Server-side equivalent of the client `customInstance.svelte.ts`.
//
// Attaches the caller's access token (from the session) to every call to the
// .NET API when present. Does NOT throw when there is no token — public
// endpoints (outlet/facility browsing) are reachable unauthenticated.
import { getRequestEvent } from "$app/server";
import { error } from "@sveltejs/kit";
import { API_URL } from "$app/env/private";

export async function customServerInstance<T>(url: string, init: RequestInit = {}): Promise<T> {
	const accessToken = getRequestEvent().locals.accessToken;

	const res = await fetch(`${API_URL}${url}`, {
		...init,
		headers: {
			...(init.headers ?? {}),
			...(accessToken ? { authorization: `Bearer ${accessToken}` } : {}),
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

export default customServerInstance;

export type ErrorType<ErrorData> = ErrorData;
export type BodyType<BodyData> = BodyData;
