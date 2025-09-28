import { type AccountMeResponse } from "$lib/api";
import { PUBLIC_API_URL } from "$env/static/public";

class User {
	value: AccountMeResponse | undefined = $state(undefined);
	isLoggedIn: boolean = $derived(this.value != undefined);
	isLoading: boolean = $state(false);

	clear() {
		this.value = undefined;
	}

	refresh() {
		this.isLoading = true;
		return fetch(`${PUBLIC_API_URL}/account/refresh`, {
			method: "POST",
			credentials: "include",
		})
			.then(async (response) => {
				if (!response.ok) {
					this.clear();
				}
			})
			.catch(() => {
				this.clear();
			})
			.finally(() => {
				this.isLoading = false;
			});
	}
}

export const user = new User();
