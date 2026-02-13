<script lang="ts">
	import { resolve } from "$app/paths";
	import type { Outlet } from "$lib/api";
	import { Badge, Button, Card } from "@kayord/ui";
	import { ClockIcon, MapPinIcon, StarIcon } from "@lucide/svelte";

	type Props = {
		outlet: Outlet;
	};

	let { outlet }: Props = $props();
</script>

<a href={resolve(`/club/${outlet.id}`)}>
	<Card.Root class="flex w-full cursor-pointer pt-0 transition-all duration-200 hover:shadow-lg">
		<div class="relative aspect-video overflow-hidden rounded-t-lg">
			<img
				src={`https://picsum.photos/seed/${outlet.id}/200/300`}
				alt={outlet.name}
				class="h-full w-full object-cover"
			/>
		</div>

		<Card.Header class="pb-3">
			<div class="flex items-start justify-between">
				<div>
					<Card.Title class="text-lg">{outlet.name}</Card.Title>
					<Card.Description class="mt-1 flex items-center">
						<MapPinIcon class="mr-1 h-4 w-4" />
						{outlet.displayName}
					</Card.Description>
				</div>
				<div class="flex items-center space-x-1">
					<StarIcon class="h-4 w-4 fill-yellow-400 text-yellow-400" />
					<span class="text-sm font-medium">{5}</span>
				</div>
			</div>
		</Card.Header>

		<Card.Content class="pt-0">
			<p class="text-muted-foreground mb-3 text-sm text-pretty">{outlet.displayName}</p>

			<div class="mb-4 flex flex-wrap gap-2">
				{#each outlet.facilities as facility (facility.id)}
					<Badge variant="secondary" class="text-xs">
						{facility}
					</Badge>
				{/each}
			</div>

			<div class="flex items-center justify-between">
				<div class="text-muted-foreground flex items-center text-sm">
					<ClockIcon class="mr-1 h-4 w-4" />
				</div>
				<Button size="sm" href={`/club/${outlet.id}`}>Select Club</Button>
			</div>
		</Card.Content>
	</Card.Root>
</a>
