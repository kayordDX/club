<script lang="ts">
	import { Button } from "@kayord/ui";
	import { page } from "$app/state";
	import { useUser } from "$lib/auth";

	let { returnUrl }: { returnUrl?: string } = $props();

	const user = useUser();
	// Default to the current location so login returns the user to where they were.
	const target = $derived(returnUrl ?? `${page.url.pathname}${page.url.search}${page.url.hash}`);
	const href = $derived(`/auth/login?next=${encodeURIComponent(target)}`);
</script>

{#if !user}
	<Button {href}>Login</Button>
{/if}
