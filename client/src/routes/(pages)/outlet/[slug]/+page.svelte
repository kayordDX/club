<script lang="ts">
	import { page } from "$app/state";
	import { createOutletGetBasic } from "$lib/api";
	import { Button } from "@kayord/ui";
	import Facility from "./Facility.svelte";
	import FacilityFilter from "./FacilityFilter.svelte";
	import Query from "$lib/components/Query.svelte";
	import { Building2Icon } from "@lucide/svelte";
	import { resolve } from "$app/paths";
	import Breadcrumbs from "./Breadcrumbs.svelte";

	const query = createOutletGetBasic(
		() => page.params.slug ?? "",
		() => ({ query: { staleTime: 1000 * 60 * 5 } })
	);
	const outlet = $derived(query.data);

	let facilityTypeIdFilter = $state("0");

	const facilitiesFiltered = $derived(
		facilityTypeIdFilter === "0"
			? (outlet?.facilities ?? [])
			: (outlet?.facilities.filter((f) => f.facilityTypeId.toString() === facilityTypeIdFilter) ??
					[])
	);
</script>

<div class="m-2">
	<Breadcrumbs />
	<Query {query} emptyText="Unable to load outlet">
		<div class="flex items-center justify-between">
			<div>
				<h1 class="text-3xl">Choose your facility</h1>
				<h3 class="text-muted-foreground mb-6">Select facility to continue with your booking.</h3>
			</div>
			<div>
				<Button href={resolve(`/outlet/${page.params.slug}/info`)} variant="outline">
					<Building2Icon /> Outlet
				</Button>
			</div>
		</div>

		<FacilityFilter bind:facilityTypeIdFilter facilities={outlet!.facilities} />
		<div class="flex flex-col gap-2">
			{#each facilitiesFiltered as facility (facility.id)}
				<Facility {facility} />
			{/each}
		</div>
	</Query>
</div>
