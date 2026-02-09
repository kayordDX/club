import { getError, isValidationError } from "$lib/types";
import { PUBLIC_API_URL } from "$env/static/public";
import qs from "qs";
import { user } from "$lib/stores/user.svelte";
import { getCookie } from "$lib/util";
import { SvelteDate } from "svelte/reactivity";

export const customInstance = async <T>({
	url,
	method,
	params,
	headers,
	data,
	responseType = "json",
}: {
	url: string;
	method: "GET" | "POST" | "PUT" | "DELETE" | "PATCH";
	params?: Record<string, any>;
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

	// TODO: check cookie expiry and refresh if it expires soon
	$effect.root(() => {
		$effect(() => {
			if (!user.isLoading) {
				const hasTokenCookie = getCookie("HAS_TOKEN");
				if (hasTokenCookie) {
					const expires = new SvelteDate(hasTokenCookie);
					const now = new SvelteDate();
					const diff = expires.getTime() - now.getTime();
					const expireCheck = 2 * 60 * 1000;

					// Token will expire in next 2 minutes
					if (diff <= expireCheck) {
						console.log("Refreshing token...");
						user.refresh();
					}
				}
			}
		});
	});

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
		if (responseType === "blob") return (await response.blob()) as unknown as T;
		if (responseType === "text") return (await response.text()) as unknown as T;
		return response.json();
	} else {
		if (response.status == 401) {
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
