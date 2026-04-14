<script lang="ts">
	import { Button } from "@kayord/ui";
	import { auth } from "$lib/stores/auth.svelte";
	import { PUBLIC_IDENTITY_URL } from "$env/static/public";

	// Temp remove this
	let error: string | null = null;
</script>

<div class="flex min-h-screen flex-col items-center justify-center gap-6 p-8">
	<h1 class="text-2xl font-bold">OIDC Test Page</h1>
	<p class="text-muted-foreground text-sm">Authority: {PUBLIC_IDENTITY_URL}</p>

	{#if auth.isLoading}
		<p class="text-muted-foreground text-sm">Loading session...</p>
	{:else if error}
		<p class="text-destructive text-sm">Error: {error}</p>
		<Button onclick={auth.login}>Try Login</Button>
	{:else if auth.user && !auth.user.expired}
		<div class="bg-card flex w-full max-w-md flex-col gap-3 rounded-lg border p-6">
			<h2 class="font-semibold">Logged in</h2>
			<div class="grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
				<span class="text-muted-foreground">Subject</span>
				<span class="font-mono">{auth.user.profile.sub}</span>
				{#if auth.user.profile.name}
					<span class="text-muted-foreground">Name</span>
					<span>{auth.user.profile.name}</span>
				{/if}
				{#if auth.user.profile.email}
					<span class="text-muted-foreground">Email</span>
					<span>{auth.user.profile.email}</span>
				{/if}
				<span class="text-muted-foreground">Expires</span>
				<span>{new Date((auth.user.expires_at ?? 0) * 1000).toLocaleString()}</span>
			</div>
			<details class="mt-2">
				<summary class="text-muted-foreground cursor-pointer text-sm">Access token</summary>
				<pre class="bg-muted mt-2 overflow-x-auto rounded p-2 text-xs">{auth.user
						.access_token}</pre>
			</details>
		</div>
		<Button variant="outline" onclick={auth.logout}>Logout</Button>
	{:else}
		<p class="text-muted-foreground text-sm">Not logged in.</p>
		<Button onclick={auth.login}>Login with OpenIddict</Button>
	{/if}
</div>

{auth.isLoading}
{auth.isAuthenticated}
