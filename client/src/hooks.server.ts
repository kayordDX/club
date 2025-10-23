import type { UserJWT } from "$lib/types";
import { jwtDecode } from "jwt-decode";

export async function handle({ event, resolve }) {
	console.log("handle hook called", new Date().toISOString(), event.route);

	const accessToken = event.cookies.get("ACCESS_TOKEN");
	if (accessToken) {
		const decodedUser = jwtDecode<UserJWT>(accessToken);
		event.locals.user = decodedUser;
	}
	return await resolve(event);
}
