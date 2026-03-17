<script lang="ts">
	import { page } from "$app/state";

	const slug = $derived(page.params.slug ?? "");
	const facilityId = $derived(page.params.id ?? "");
	const selectedDate = $derived(page.url.searchParams.get("date"));
	const selectedSlot = $derived(page.url.searchParams.get("slotId"));
	const quantity = $derived(page.url.searchParams.get("quantity") ?? "1");

	const confirmHref = $derived(createBookingStepHref(slug, facilityId, "confirm", page.url.search));
</script>

Test
