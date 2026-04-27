<script>
	import { Button } from "@kayord/ui";
	import Search from "./Search.svelte";
	import { createOutletGetAll, createTest } from "$lib/api";
	import Outlet from "./Outlet.svelte";
	import LoginButton from "$lib/components/LoginButton/LoginButton.svelte";

	const query = createOutletGetAll();
	let data = $derived(query.data?.items ?? []);

	const test = createTest(() => ({ name: "test" }));
</script>

<Button onclick={() => test.refetch()}>Test</Button>

<main class="container mx-auto px-4 py-8">
	<div class="mb-12 text-center">
		<h1 class="text-foreground mb-4 text-4xl font-bold text-balance">Book Your Perfect Game</h1>
		<p class="text-muted-foreground mx-auto max-w-2xl text-lg text-pretty">
			Reserve paddle courts and golf slots at premium clubs. Select your preferred club to view
			available times and make your booking.
		</p>
		<LoginButton />
	</div>
	<Search />
	<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-2">
		{#each data as outlet (outlet.id)}
			<Outlet {outlet} />
		{/each}
	</div>
</main>
