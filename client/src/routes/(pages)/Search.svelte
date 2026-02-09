<script lang="ts">
	import { Card, Input, Button, Badge } from "@kayord/ui";
	import { CalendarIcon, ClockIcon, MapPinIcon, SearchIcon } from "@lucide/svelte";

	const handleSearch = () => console.log("searching");

	let searchQuery = $state("");
	const selectedFilters = $state<Array<string>>([]);

	const quickFilters = [
		"Paddle Courts",
		"Golf Courses",
		"Available Today",
		"Premium Clubs",
		"Budget Friendly",
	];

	const popularSearches = [
		{ term: "Paddle courts near me", icon: MapPinIcon },
		{ term: "Golf slots today", icon: CalendarIcon },
		{ term: "Morning bookings", icon: ClockIcon },
		{ term: "Premium clubs", icon: SearchIcon },
	];
</script>

<div class="mb-16">
	<Card.Root class="bg-card/50 border-border/50 backdrop-blur-sm">
		<Card.Content class="p-8">
			<form onsubmit={handleSearch} class="mb-6">
				<div class="relative">
					<SearchIcon
						class="text-muted-foreground absolute top-1/2 left-4 h-5 w-5 -translate-y-1/2 transform"
					/>
					<Input
						type="text"
						placeholder="Search clubs, locations, or sports..."
						bind:value={searchQuery}
						class="bg-background/50 border-border/50 focus:border-primary/50 py-6 pr-4 pl-12 text-lg"
					/>
					<Button type="submit" class="absolute top-1/2 right-2 -translate-y-1/2 transform px-6">
						<SearchIcon />
						Search
					</Button>
				</div>
			</form>

			<div class="mb-6">
				<h3 class="text-muted-foreground mb-3 text-sm font-medium">Quick Filters</h3>
				<div class="flex flex-wrap gap-2">
					{#each quickFilters as filter (filter)}
						<Badge
							variant={selectedFilters.includes(filter) ? "default" : "secondary"}
							class="hover:bg-primary/20 cursor-pointer transition-colors"
						>
							{filter}
						</Badge>
					{/each}
				</div>
			</div>

			<div>
				<h3 class="text-muted-foreground mb-3 text-sm font-medium">Popular Searches</h3>
				<div class="grid grid-cols-1 gap-3 md:grid-cols-2">
					{#each popularSearches as search (search)}
						{@const Icon = search.icon}

						<button
							class="bg-background/50 hover:bg-background/80 border-border/50 hover:border-border flex items-center gap-3 rounded-lg border p-3 text-left transition-all"
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
