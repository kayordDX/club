import { getRequestEvent } from "$app/server";
import { error } from "@sveltejs/kit";
import { PUBLIC_API_URL } from "$env/static/public";

/**
 * Server-side mutator used by the orval `fetch` client output
 * (src/lib/server/api/generated/**). Attaches the caller's access token
 * (from the session, never sent to the browser) to every call to the .NET API.
 */
export const customServerInstance = async <T>(url: string, options: RequestInit = {}): Promise<T> => {
	const accessToken = getRequestEvent().locals.accessToken;
	if (!accessToken) {
		throw error(401, "Not authenticated");
	}

	const res = await fetch(`${PUBLIC_API_URL}${url}`, {
		...options,
		headers: {
			...(options.headers ?? {}),
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
};

export default customServerInstance;

export type ErrorType<ErrorData> = ErrorData;
export type BodyType<BodyData> = BodyData;
