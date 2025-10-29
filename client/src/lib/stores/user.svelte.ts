import { accountMe, refresh, type AccountMeQueryResult } from "$lib/api";

class User {
	value: AccountMeQueryResult | undefined = $state(undefined);
	isLoggedIn: boolean = $derived(this.value != undefined);
	isLoading: boolean = $state(false);

	clear() {
		this.value = undefined;
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
}

export const user = new User();
