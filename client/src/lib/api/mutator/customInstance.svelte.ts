import { getError, isValidationError } from "$lib/types";
import { PUBLIC_API_URL } from "$env/static/public";
import qs from "qs";
import { user } from "$lib/stores/user.svelte";
// import { info } from "$lib/stores/info.svelte";

export const customInstance = async <T>({
	url,
	method,
	params,
	headers,
	data,
	// eslint-disable-next-line @typescript-eslint/no-unused-vars
	responseType,
}: {
	url: string;
	method: "GET" | "POST" | "PUT" | "DELETE" | "PATCH";
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	params?: Record<string, any>;
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	headers?: Record<string, any>;
	data?: BodyType<unknown>;
	responseType?: string;
}): Promise<T> => {
	let fullUrl = `${PUBLIC_API_URL}${url}`;
	if (params !== undefined) {
		const urlParams = qs.stringify(params);
		if (urlParams.length > 0) {
			fullUrl = fullUrl + "?" + urlParams;
		}
	}

	// const token = (await session.user?.getIdToken()) ?? "";
	// if (headers == undefined) {
	// 	if (session.user != undefined) {
	// 		headers = {
	// 			Authorization: `Bearer ${token}`,
	// 		};
	// 	}
	// }

	// if (headers) {
	// 	if (headers.Authorization == undefined) {
	// 		headers.Authorization = `Bearer ${token}`;
	// 	}
	// }

	const response = await fetch(fullUrl, {
		method,
		headers: {
			...headers,
		},
		...(data ? { body: JSON.stringify(data) } : {}),
		credentials: "include",
	});

	if (response.ok) {
		if (response.status == 204) return null as unknown as T;
		return response.json();
	} else {
		if (response.status == 401) {
			await user.refresh();
			throw new Error("Unauthorized", { cause: "401" });
		}
		if (response.status == 403) {
			throw new Error("Forbidden", { cause: "403" });
		}
		if (response.status == 404) {
			throw new Error("Not found", { cause: "404" });
		}
		// Error response
		const errorResult = await response.json();
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

export default customInstance;

export type ErrorType<ErrorData> = ErrorData;

export type BodyType<BodyData> = BodyData;
