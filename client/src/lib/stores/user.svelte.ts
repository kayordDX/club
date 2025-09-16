import { type AccountMeQueryResult } from "$lib/api";

class User {
	value: AccountMeQueryResult | undefined = $state(undefined);
	isLoggedIn: boolean = $derived(this.value != undefined);
	isLoading: boolean = $state(false);

	clear() {
		this.value = undefined;
	}
}

export const user = new User();
