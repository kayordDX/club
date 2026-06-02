<script lang="ts">
	import type { FacilityBasicDTO } from "$lib/api";
	import { Badge, ToggleGroup } from "@kayord/ui";
	import FacilityIcon from "$lib/components/Icons/FacilityIcon.svelte";
	import { Grid2x2Icon } from "@lucide/svelte";

	type Props = {
		facilities: Array<FacilityBasicDTO>;
		facilityTypeIdFilter: string;
	};

	let { facilities, facilityTypeIdFilter = $bindable() }: Props = $props();

	// Get count of facilities for a specific type
	const getFacilityTypeCount = $derived(
		(typeId: number) => facilities.filter((f) => f.facilityTypeId === typeId).length
	);
</script>

{#if facilities.length > 0}
	<ToggleGroup.Root
		type="single"
		onValueChange={(value) =>
			value == "" ? (facilityTypeIdFilter = "0") : (facilityTypeIdFilter = value)}
		spacing={2}
		class="mb-2 flex-wrap"
		bind:value={facilityTypeIdFilter}
		variant="outline"
	>
		<ToggleGroup.Item value="0" aria-label="Toggle all">
			<Grid2x2Icon />
			All <span class="hidden md:flex">Facilities</span>
			<Badge variant="outline" class="hidden md:flex">
				{facilities.length}
			</Badge>
		</ToggleGroup.Item>
		{#each facilities as facility (facility.id)}
			<ToggleGroup.Item
				value={facility.facilityTypeId.toString()}
				aria-label={`Toggle ${facility.facilityTypeName}`}
			>
				<FacilityIcon typeId={facility.facilityTypeId} />
				{facility.facilityTypeName}
				<Badge variant="outline" class="hidden md:flex">
					{getFacilityTypeCount(facility.facilityTypeId)}
				</Badge>
			</ToggleGroup.Item>
		{/each}
	</ToggleGroup.Root>
{/if}
