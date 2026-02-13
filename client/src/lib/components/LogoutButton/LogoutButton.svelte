<script lang="ts">
	import { Button } from "@kayord/ui";
	import { user } from "$lib/stores/user.svelte";
	import { createLogout } from "$lib/api";
	import { resolve } from "$app/paths";
	import { goto } from "$app/navigation";
	import { LogOutIcon } from "@lucide/svelte";

	const logoutMut = createLogout();
	const logout = async () => {
		try {
			isLoading = true;
			await logoutMut.mutateAsync();
			user.clear();
			goto(resolve("/"));
		} finally {
			isLoading = false;
		}
	};

	let isLoading = $state(false);
</script>

{#if user.isLoggedIn}
	<Button onclick={logout} disabled={isLoading} variant="destructive">
		<LogOutIcon />
		Logout
	</Button>
{/if}
