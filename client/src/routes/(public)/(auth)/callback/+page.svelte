<script lang="ts">
	import { goto } from "$app/navigation";
	import { auth } from "$lib/stores/auth.svelte";
	import { onMount } from "svelte";

	let error = $state<string | null>(null);

	onMount(async () => {
		try {
			const user = await auth.handleCallback();
			// Restore the page the user was on before step-up auth redirected them away.
			// Falls back to "/" for normal login flows that don't set a returnUrl.
			const returnUrl = (user.state as { returnUrl?: string } | undefined)?.returnUrl ?? "/";
			goto(returnUrl);
		} catch (err) {
			console.error("Authentication callback failed:", err);
			error = (err as Error).message;
		}
	});
</script>

<div class="flex min-h-screen flex-col items-center justify-center gap-4 p-8">
	{#if error}
		<p class="text-destructive text-sm">Callback error: {error}</p>
	{:else}
		<p class="text-muted-foreground text-sm">Completing sign-in...</p>
	{/if}
</div>
