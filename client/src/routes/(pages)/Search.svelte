<script lang="ts">
	import { Card, Badge, InputGroup } from "@kayord/ui";
	import { CalendarIcon, ClockIcon, MapPinIcon, SearchIcon } from "@lucide/svelte";

	type Props = {
		draft?: string;
		onsearch: () => void;
	};

	let { draft = $bindable(""), onsearch }: Props = $props();

	function handleSubmit(event: SubmitEvent) {
		event.preventDefault();
		onsearch();
	}

	// Selecting a quick filter / popular search commits it immediately.
	function select(term: string) {
		draft = term;
		onsearch();
	}

	// Each quick filter maps to a free-text search term resolved by the backend.
	const quickFilters = [
		{ label: "Paddle Courts", term: "paddle" },
		{ label: "Golf Courses", term: "golf" },
		{ label: "Premium Clubs", term: "premium" },
		{ label: "Budget Friendly", term: "budget" },
	];

	const popularSearches = [
		{ term: "Paddle courts near me", icon: MapPinIcon, query: "paddle" },
		{ term: "Golf slots today", icon: CalendarIcon, query: "golf" },
		{ term: "Morning bookings", icon: ClockIcon, query: "morning" },
		{ term: "Premium clubs", icon: SearchIcon, query: "premium" },
	];
</script>

<div class="mb-16">
	<Card.Root class="bg-card/50 border-border/50 backdrop-blur-sm">
		<Card.Content class="p-8">
			<form onsubmit={handleSubmit} class="mb-6">
				<InputGroup.Root class="min-h-12">
					<InputGroup.Input placeholder="Search clubs, sports or locations..." bind:value={draft} />
					<InputGroup.Addon>
						<SearchIcon />
					</InputGroup.Addon>
					<InputGroup.Addon align="inline-end">
						<InputGroup.Button type="submit" variant="default" class="min-h-10">
							<SearchIcon />Search
						</InputGroup.Button>
					</InputGroup.Addon>
				</InputGroup.Root>
			</form>

			<div class="mb-6">
				<h3 class="text-muted-foreground mb-3 text-sm font-medium">Quick Filters</h3>
				<div class="flex flex-wrap gap-2">
					{#each quickFilters as filter (filter.label)}
						<Badge variant="secondary" class="hover:bg-primary/20 cursor-pointer transition-colors" onclick={() => select(filter.term)}>
							{filter.label}
						</Badge>
					{/each}
				</div>
			</div>

			<div>
				<h3 class="text-muted-foreground mb-3 text-sm font-medium">Popular Searches</h3>
				<div class="grid grid-cols-1 gap-3 md:grid-cols-2">
					{#each popularSearches as search (search.term)}
						{@const Icon = search.icon}

						<button
							type="button"
							class="bg-background/50 hover:bg-background/80 border-border/50 hover:border-border flex items-center gap-3 rounded-lg border p-3 text-left transition-all"
							onclick={() => select(search.query)}
						>
							<Icon class="text-primary h-4 w-4" />
							<span class="text-foreground text-sm">{search.term}</span>
						</button>
					{/each}
				</div>
			</div>
		</Card.Content>
	</Card.Root>
</div>
