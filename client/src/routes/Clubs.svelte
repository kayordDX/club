<script lang="ts">
	import { goto } from "$app/navigation";
	import { Badge, Button, Card } from "@kayord/ui";
	import { ClockIcon, MapPinIcon, StarIcon } from "@lucide/svelte";

	interface Club {
		id: string;
		name: string;
		location: string;
		rating: number;
		facilities: string[];
		description: string;
		priceRange: string;
	}

	const clubs: Club[] = [
		{
			id: "1",
			name: "Royal Sports Club",
			location: "Downtown District",
			rating: 4.8,
			facilities: ["Paddle Courts", "Golf Course", "Pro Shop"],
			description:
				"Premium sports facility with world-class amenities and professional instruction.",
			priceRange: "$80-120/hour",
		},
		{
			id: "2",
			name: "Green Valley Golf & Paddle",
			location: "Suburban Hills",
			rating: 4.6,
			facilities: ["18-Hole Golf", "Paddle Courts", "Restaurant"],
			description: "Beautiful parkland setting with challenging golf and modern paddle facilities.",
			priceRange: "$60-90/hour",
		},
		{
			id: "3",
			name: "City Sports Complex",
			location: "Business District",
			rating: 4.4,
			facilities: ["Paddle Courts", "Driving Range", "Fitness Center"],
			description:
				"Convenient urban location with state-of-the-art paddle courts and golf practice.",
			priceRange: "$45-75/hour",
		},
		{
			id: "4",
			name: "Oceanview Country Club",
			location: "Coastal Area",
			rating: 4.9,
			facilities: ["Championship Golf", "Paddle Courts", "Spa"],
			description: "Exclusive oceanfront club offering breathtaking views and exceptional service.",
			priceRange: "$100-150/hour",
		},
	];

	let selectedClub = $state<string | null>(null);

	const handleClubSelect = (clubId: string) => {
		selectedClub = clubId;
	};
</script>

<div class="space-y-8">
	<div class="text-center">
		<h2 class="text-foreground mb-2 text-2xl font-semibold">Choose Your Club</h2>
		<p class="text-muted-foreground">Select from our premium partner facilities</p>
	</div>

	<button class="flex w-full justify-center" onclick={() => goto("/club")}>
		<div class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-2">
			{#each clubs as club (club.id)}
				<Card.Root
					class={`cursor-pointer transition-all duration-200 hover:shadow-lg ${
						selectedClub === club.id ? "ring-primary ring-2" : ""
					}`}
					onclick={() => handleClubSelect(club.id)}
				>
					<div class="relative aspect-video overflow-hidden rounded-t-lg">
						<img
							src={`https://picsum.photos/seed/${club.id}/200/300`}
							alt={club.name}
							class="h-full w-full object-cover"
						/>
					</div>

					<Card.Header class="pb-3">
						<div class="flex items-start justify-between">
							<div>
								<Card.Title class="text-lg">{club.name}</Card.Title>
								<Card.Description class="mt-1 flex items-center">
									<MapPinIcon class="mr-1 h-4 w-4" />
									{club.location}
								</Card.Description>
							</div>
							<div class="flex items-center space-x-1">
								<StarIcon class="h-4 w-4 fill-yellow-400 text-yellow-400" />
								<span class="text-sm font-medium">{club.rating}</span>
							</div>
						</div>
					</Card.Header>

					<Card.Content class="pt-0">
						<p class="text-muted-foreground mb-3 text-pretty text-sm">{club.description}</p>

						<div class="mb-4 flex flex-wrap gap-2">
							{#each club.facilities as facility}
								<Badge variant="secondary" class="text-xs">
									{facility}
								</Badge>
							{/each}
						</div>

						<div class="flex items-center justify-between">
							<div class="text-muted-foreground flex items-center text-sm">
								<ClockIcon class="mr-1 h-4 w-4" />
								{club.priceRange}
							</div>
							<Button size="sm" class={selectedClub === club.id ? "bg-primary" : ""}>
								{selectedClub === club.id ? "Selected" : "Select Club"}
							</Button>
						</div>
					</Card.Content>
				</Card.Root>
			{/each}
		</div>
	</button>
</div>
