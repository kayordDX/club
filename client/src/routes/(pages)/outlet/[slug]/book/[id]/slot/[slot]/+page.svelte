<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import { Button } from "@kayord/ui";

	// const slug = $derived(page.params.slug ?? "");
	const id = $derived(page.params.slot ?? "");
	// const facilityId = $derived(page.params.id ?? "");

	// Get search params directly from page.url
	// const id = $derived(page.url.searchParams.get("id") ?? "");
	const slotCount = $derived(Number(page.url.searchParams.get("slotCount")) || 1);
</script>

Test Id: {id}
<div>Slot Count: {slotCount}</div>

<a href={resolve(`/outlet/${page.params.slug}/book/${page.params.id}/booking/${id}/confirm`)}
	>Book</a
>
