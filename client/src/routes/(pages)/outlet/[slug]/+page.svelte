<script lang="ts">
	import { page } from "$app/state";
	import { outletGetBasic } from "$lib/api/remote/outlet.remote";
	import { Button } from "@kayord/ui";
	import Facility from "./Facility.svelte";
	import FacilityFilter from "./FacilityFilter.svelte";
	import Await from "$lib/components/Await.svelte";
	import { Building2Icon } from "@lucide/svelte";
	import { resolve } from "$app/paths";
	import Breadcrumbs from "./Breadcrumbs.svelte";

	const outlet = outletGetBasic(page.params.slug ?? "");

	let facilityTypeIdFilter = $state("0");
</script>

<div class="m-2">
	<Breadcrumbs />
	<Await promise={outlet} emptyText="Unable to load outlet">
		{#snippet children(o)}
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

			<FacilityFilter bind:facilityTypeIdFilter facilities={o.facilities} />
			<div class="flex flex-col gap-2">
				{#each facilityTypeIdFilter === "0" ? o.facilities : o.facilities.filter((f) => f.facilityTypeId.toString() === facilityTypeIdFilter) as facility (facility.id)}
					<Facility {facility} />
				{/each}
			</div>
		{/snippet}
	</Await>
</div>
