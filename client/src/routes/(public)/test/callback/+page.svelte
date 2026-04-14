<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { auth } from "$lib/stores/auth.svelte";
	import { onMount } from "svelte";

	let error = $state<string | null>(null);

	onMount(async () => {
		try {
			await auth.handleCallback();
			goto("/test");
		} catch (err) {
			console.error("Authentication callback failed:", err);
			error = (err as Error).message;
			// Optional: Redirect to a login-error page
			// goto("/login?error=callback_failed");
		}
	});
</script>

<div class="flex min-h-screen flex-col items-center justify-center gap-4 p-8">
	{#if error}
		<p class="text-destructive text-sm">Callback error: {error}</p>
		<a href={resolve("/test")} class="text-primary text-sm underline">Back to test page</a>
	{:else}
		<p class="text-muted-foreground text-sm">Completing sign-in...</p>
	{/if}
</div>
