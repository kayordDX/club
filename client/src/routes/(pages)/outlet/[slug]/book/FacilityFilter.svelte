<script lang="ts">
	import type { FacilityDTO } from "$lib/api";
	import { Badge, ToggleGroup } from "@kayord/ui";
	import FacilityIcon from "$lib/components/Icons/FacilityIcon.svelte";
	import { Grid2X2Icon } from "@lucide/svelte";

	type Props = {
		facilities: Array<FacilityDTO>;
	};

	let { facilities }: Props = $props();

	// Get count of facilities for a specific type
	const getFacilityTypeCount = $derived(
		(typeId: number) => facilities.filter((f) => f.facilityType.id === typeId).length
	);
</script>

{#if facilities.length > 0}
	<ToggleGroup.Root type="single" spacing={2} class="mb-2" value="0" variant="outline">
		<ToggleGroup.Item value="0" aria-label="Toggle all">
			<Grid2X2Icon />
			All Facilities <Badge variant="outline">{facilities.length}</Badge>
		</ToggleGroup.Item>
		{#each facilities as facility (facility.id)}
			<ToggleGroup.Item
				value={facility.facilityType.id.toString()}
				aria-label={`Toggle ${facility.facilityType.name}`}
			>
				<FacilityIcon typeId={facility.facilityType.id} />
				{facility.facilityType.name}
				<Badge variant="outline">{getFacilityTypeCount(facility.facilityType.id)}</Badge>
			</ToggleGroup.Item>
		{/each}
	</ToggleGroup.Root>
{/if}
