import { getRequestEvent, query } from "$app/server";
import type { UserJWT } from "$lib/types";
import { jwtDecode } from "jwt-decode";

export const getUserCookies = query(async () => {
	console.log("getUserCookies called", new Date().toISOString());
	const { cookies } = getRequestEvent();
	const accessToken = cookies.get("ACCESS_TOKEN");
	if (accessToken) {
		const decodedUser = jwtDecode<UserJWT>(accessToken);
		return decodedUser;
	}
	return undefined;
});
