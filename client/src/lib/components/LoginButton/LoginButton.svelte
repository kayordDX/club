<script lang="ts">
	import { Button } from "@kayord/ui";
	import { page } from "$app/state";
	import { auth } from "$lib/stores/auth.svelte";

	let { returnUrl }: { returnUrl?: string } = $props();

	// Default to the current location (path, query and hash) so login returns the
	// user to where they were. Callers can pass an explicit returnUrl to override.
	const target = $derived(returnUrl ?? `${page.url.pathname}${page.url.search}${page.url.hash}`);
</script>

{#if !auth.isAuthenticated}
	<Button onclick={() => auth.login(target)} disabled={auth.isLoading}>Login</Button>
{/if}
