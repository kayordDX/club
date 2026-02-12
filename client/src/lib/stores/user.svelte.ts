import { accountMe, refresh, type AccountMeQueryResult } from "$lib/api";
import { getCookie } from "$lib/util";
import { SvelteDate } from "svelte/reactivity";

class User {
	#lastCheck: number = 0;
	value: AccountMeQueryResult | undefined = $state(undefined);
	isLoggedIn: boolean = $derived(this.value != undefined);
	isLoading: boolean = $state(false);

	clear() {
		this.value = undefined;
		this.#lastCheck = 0;
	}

	async update() {
		try {
			this.isLoading = true;
			const user = await accountMe();
			this.value = user;
		} catch {
			this.clear();
		} finally {
			this.isLoading = false;
		}
		return this.value;
	}

	async refresh() {
		try {
			this.isLoading = true;
			const user = await refresh();
			this.value = user;
		} catch {
			this.clear();
		} finally {
			this.isLoading = false;
		}
	}

	async checkCookie() {
		// Only check cookie every 2 minutes to avoid excessive checks
		if (this.#lastCheck < Date.now() - 2 * 60 * 1000) {
			this.#lastCheck = Date.now();

			const hasTokenCookie = getCookie("HAS_TOKEN");

			if (hasTokenCookie) {
				const expires = new SvelteDate(hasTokenCookie);
				const now = new SvelteDate();
				const diff = expires.getTime() - now.getTime();
				// Cookie expires in next 2 minutes
				const expireCheck = 2 * 60 * 1000;

				// Token will expire in next 2 minutes
				if (diff <= expireCheck) {
					console.log("Refreshing token...");
					await user.refresh();
				}
			}
		}
	}
}

export const user = new User();
