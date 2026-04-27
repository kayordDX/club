<script lang="ts">
	import { Button } from "@kayord/ui";
	import { LogOutIcon } from "@lucide/svelte";
	import { auth } from "$lib/stores/auth.svelte";
</script>

{#if auth.isAuthenticated}
	<Button onclick={auth.logout} disabled={auth.isLoading} variant="destructive">
		<LogOutIcon />
		Logout
	</Button>
{/if}
