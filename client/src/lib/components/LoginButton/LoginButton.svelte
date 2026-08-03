<script lang="ts">
	import { Button } from "@kayord/ui";
	import { auth } from "$lib/stores/auth.svelte";

	let { returnUrl }: { returnUrl?: string } = $props();
</script>

{#if !auth.isAuthenticated}
	<Button onclick={() => auth.login(returnUrl)} disabled={auth.isLoading}>Login</Button>
{/if}
