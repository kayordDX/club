<script lang="ts">
	import { outletGetAll } from "$lib/api/remote/outlet.remote.js";
	import { resolve } from "$app/paths";
	import { Button } from "@kayord/ui";

	let { data } = $props();
</script>

<svelte:head>
	<title>My bookings — remote function demo</title>
</svelte:head>

<Button onclick={() => outletGetAll().refresh()}>Refresh</Button>
<div class="m-4 space-y-4">
	<div class="flex items-center justify-between">
		<h1 class="text-2xl font-semibold">My bookings</h1>
		<div class="flex gap-2">
			<Button href={resolve("/")} variant="outline">Back to app</Button>
			<Button href="/auth/logout" variant="ghost">Sign out</Button>
		</div>
	</div>

	<p class="text-muted-foreground text-sm">
		Signed in as <strong>{data.user.firstName} {data.user.lastName}</strong> ({data.user.email}). This list was fetched <strong>server-side</strong> via a remote
		function using your session token — no token is present in the browser. View page source to confirm the data is in the HTML.
	</p>
</div>

<svelte:boundary>
	<!-- 1. Error Fallback: Shown if the query throws -->
	{#snippet failed(error, reset)}
		<button onclick={reset}>oops! try again</button>
	{/snippet}

	<!-- 2. Loading Fallback: Shown while top-level await is pending -->
	{#snippet pending()}
		<div class="skeleton">Loading user data...</div>
	{/snippet}

	<!-- 3. Top-Level Await inside boundary -->
	{@const user = await outletGetAll()}

	<article class="user-card">
		<h2>{user.hasNextPage}</h2>
		<p>{user.items.length}</p>
		{JSON.stringify(user)}
	</article>
</svelte:boundary>
