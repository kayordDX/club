<script lang="ts">
	import { page } from "$app/state";
	import { createOutletGet } from "$lib/api";
	import { Breadcrumb } from "@kayord/ui";
	import Facility from "./Facility.svelte";
	import FacilityFilter from "./FacilityFilter.svelte";
	import Query from "$lib/components/Query.svelte";
	import { HouseIcon } from "@lucide/svelte";

	const query = createOutletGet(
		() => page.params.slug ?? "",
		() => ({ query: { staleTime: 1000 * 60 * 5 } })
	);
	const outlet = $derived(query.data);

	let facilityTypeIdFilter = $state("0");

	const facilitiesFiltered = $derived(
		facilityTypeIdFilter === "0"
			? (outlet?.facilities ?? [])
			: (outlet?.facilities.filter((f) => f.facilityType.id.toString() === facilityTypeIdFilter) ??
					[])
	);
</script>

<div class="m-2">
	<Query {query} emptyText="Unable to load outlet">
		<Breadcrumb.Root class="mb-4">
			<Breadcrumb.List>
				<Breadcrumb.Item>
					<Breadcrumb.Link href="/">
						<HouseIcon class="size-3" />
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Page class="text-xs">{outlet!.name}</Breadcrumb.Page>
				</Breadcrumb.Item>
			</Breadcrumb.List>
		</Breadcrumb.Root>

		<h1 class="text-3xl">Choose your facility</h1>
		<h3 class="text-muted-foreground mb-6">Select facility to continue with your booking.</h3>

		<FacilityFilter bind:facilityTypeIdFilter facilities={outlet!.facilities} />
		<div class="flex flex-col gap-2">
			{#each facilitiesFiltered as facility (facility.id)}
				<Facility {facility} />
			{/each}
		</div>
	</Query>
</div>
