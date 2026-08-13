<script lang="ts">
	import Search from "./Search.svelte";
	import { outletGetAll } from "$lib/api/remote/outlet.remote";
	import Outlet from "./Outlet.svelte";
	import LoginButton from "$lib/components/LoginButton/LoginButton.svelte";
	import { Skeleton } from "@kayord/ui";

	let searchTerm = $state("");
	let draft = $state("");

	// `draft` is what the input holds; `searchTerm` is the committed value sent to
	// the backend so we only search on submit, not on every keystroke.
	function commitSearch() {
		searchTerm = draft.trim();
	}

	function clearSearch() {
		searchTerm = "";
		draft = "";
	}

	// Reactive remote query — re-fetches when the committed search term changes.
	const outlets = $derived(outletGetAll(searchTerm ? { search: searchTerm } : undefined));
</script>

<main class="container mx-auto px-4 py-8">
	<div class="mb-12 text-center">
		<h1 class="text-foreground mb-4 text-4xl font-bold text-balance">Book Your Perfect Game</h1>
		<p class="text-muted-foreground mx-auto max-w-2xl text-lg text-pretty">
			Reserve paddle courts and golf slots at premium clubs. Select your preferred club to view available times and make your booking.
		</p>
		<LoginButton />
	</div>
	<Search bind:draft onsearch={commitSearch} />
	{#if searchTerm}
		<div class="text-muted-foreground mb-6 flex items-center justify-center gap-2 text-sm">
			<span> searching for &ldquo;{searchTerm}&rdquo; </span>
			<button class="text-primary hover:underline" onclick={clearSearch} data-testid="clear-search"> Clear </button>
		</div>
	{/if}
	{#if outlets.loading}
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-2">
			<Skeleton class="h-152 w-full" />
			<Skeleton class="h-152 w-full" />
		</div>
	{:else}
		{#if searchTerm && (await outlets).items.length === 0}
			<p class="text-muted-foreground py-12 text-center" data-testid="no-results">No clubs found matching your search.</p>
		{:else}
			<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-2">
				{#each (await outlets).items as outlet (outlet.id)}
					<Outlet {outlet} />
				{/each}
			</div>
		{/if}
	{/if}
</main>
