<script lang="ts">
	// The server-side OIDC flow handles callbacks at /auth/callback. This legacy
	// /callback route (used by the old client-side flow) just returns to the app.
	import { onMount } from "svelte";
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";

	onMount(() => {
		goto(resolve("/"));
	});
</script>

<div class="flex min-h-screen flex-col items-center justify-center gap-4 p-8">
	<p class="text-muted-foreground text-sm">Redirecting…</p>
</div>
