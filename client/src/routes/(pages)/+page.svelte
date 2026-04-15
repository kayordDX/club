<script>
	import { Button } from "@kayord/ui";
	import Search from "./Search.svelte";
	import { user } from "$lib/stores/user.svelte";
	import { createOutletGetAll, createTest } from "$lib/api";
	import Outlet from "./Outlet.svelte";
	import { goto } from "$app/navigation";

	const query = createOutletGetAll();
	let data = $derived(query.data?.items ?? []);

	const test = createTest(() => ({ name: "test" }));
	const login = () => {
		// We send the user to the backend, and tell the backend where to send them back
		window.location.href = "http://localhost:5000/auth/login?returnUrl=http://localhost:5173";
	};
</script>

<Button onclick={() => test.refetch()}>Test</Button>
<Button onclick={login}>Login</Button>
<main class="container mx-auto px-4 py-8">
	<div class="mb-12 text-center">
		<h1 class="text-foreground mb-4 text-4xl font-bold text-balance">Book Your Perfect Game</h1>
		<p class="text-muted-foreground mx-auto max-w-2xl text-lg text-pretty">
			Reserve paddle courts and golf slots at premium clubs. Select your preferred club to view
			available times and make your booking.
		</p>
		{#if !user.isLoggedIn}
			<Button class="mt-4" href="/login">Login</Button>
		{/if}
	</div>
	<Search />
	<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-2">
		{#each data as outlet (outlet.id)}
			<Outlet {outlet} />
		{/each}
	</div>
</main>
