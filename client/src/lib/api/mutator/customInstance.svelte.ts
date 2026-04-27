import { getError, isValidationError } from "$lib/types";
import { PUBLIC_API_URL } from "$env/static/public";
import { auth } from "$lib/stores/auth.svelte";

const getUrl = (contextUrl: string): string => {
	const baseUrl = PUBLIC_API_URL;
	const url = new URL(baseUrl + contextUrl);
	const pathname = url.pathname;
	const search = url.search;
	const requestUrl = new URL(`${baseUrl}${pathname}${search}`);
	return requestUrl.toString();
};

const getBody = async <T>(resp: Response): Promise<T> => {
	if (resp.ok) {
		if (resp.status == 204) return null as unknown as Promise<T>;
		const contentType = resp.headers.get("content-type");
		if (contentType && contentType.includes("application/pdf")) {
			return resp.blob() as Promise<T>;
		}
		return resp.json() as T;
	} else {
		if (resp.status == 401) {
			// TODO: Possibly refresh token from lib/firebase
			throw new Error("Unauthorized", { cause: "401" });
		}
		if (resp.status == 403) {
			throw new Error("Forbidden", { cause: "403" });
		}
		if (resp.status == 404) {
			throw new Error("Not found", { cause: "404" });
		}

		// Error response
		const errorResult = await resp.json();
		if (isValidationError(errorResult)) {
			const errorMessage = Object.values(errorResult.errors ?? []).map((e) => e.toString());
			throw new Error(errorResult.message, {
				cause: errorMessage.join("\n"),
			});
		} else {
			throw new Error(getError(errorResult).message);
		}
	}
};

const getHeaders = (token: string, headers?: HeadersInit): HeadersInit => {
	if (headers == undefined) {
		if (auth.isAuthenticated) {
			headers = {
				Authorization: `Bearer ${token}`,
			};
		}
	}
	return {
		...headers,
		Authorization: `Bearer ${token}`,
	};
};

export const customInstance = async <T>(url: string, options: RequestInit): Promise<T> => {
	const requestUrl = getUrl(url);
	const token = auth.accessToken ?? "";
	const requestHeaders = getHeaders(token, options.headers);
	const requestInit: RequestInit = {
		...options,
		headers: requestHeaders,
	};

	const request = new Request(requestUrl, requestInit);
	const response = await fetch(request);

	const data = await getBody<T>(response);
	return data as T;
};

export default customInstance;

export type ErrorType<ErrorData> = ErrorData;

export type BodyType<BodyData> = BodyData;
