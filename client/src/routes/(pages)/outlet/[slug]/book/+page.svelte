<script lang="ts">
	import { page } from "$app/state";
	import { createOutletGet } from "$lib/api";
	import { Breadcrumb } from "@kayord/ui";
	import Facility from "./Facility.svelte";
	import FacilityFilter from "./FacilityFilter.svelte";
	import Query from "$lib/components/Query.svelte";

	const query = createOutletGet(() => page.params.slug ?? "");
	const outlet = $derived(query.data);
</script>

<div class="m-4">
	<Query {query} emptyText="Unable to load outlet">
		<Breadcrumb.Root class="mb-4">
			<Breadcrumb.List>
				<Breadcrumb.Item>
					<Breadcrumb.Link href="/">Home</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Page>{outlet!.name}</Breadcrumb.Page>
				</Breadcrumb.Item>
			</Breadcrumb.List>
		</Breadcrumb.Root>

		<h1 class="text-3xl">Choose your facility</h1>
		<h3 class="text-muted-foreground mb-6">Select facility to continue with your booking.</h3>

		<FacilityFilter facilities={outlet!.facilities} />

		<div class="flex flex-col gap-2">
			{#each outlet!.facilities as facility (facility.id)}
				<Facility {facility} />
			{/each}
		</div>
	</Query>
</div>
