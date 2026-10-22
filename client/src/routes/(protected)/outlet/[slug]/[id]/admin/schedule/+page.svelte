<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { createSearchParamsSchema, useSearchParams } from "runed/kit";
	import { getLocalTimeZone, parseDate, today } from "@internationalized/date";
	import { Button, Skeleton } from "@kayord/ui";
	import { BookIcon } from "@lucide/svelte";
	import { adminSlotGetAll } from "#lib/api/remote/admin.remote.js";
	import { openAdminBookingCreation } from "#lib/booking/admin-creation-navigation.js";
	import PageBoundary from "#lib/components/PageBoundary.svelte";
	import ScheduleDateNavigation from "#lib/components/schedule/schedule-date-navigation.svelte";
	import Slots from "#lib/components/schedule/slots.svelte";

	const facilityId = $derived(Number(page.params.id) || 0);
	const params = useSearchParams(createSearchParamsSchema({ date: { type: "string", default: today(getLocalTimeZone()).toString() } }), { noScroll: true });
	const selectedDate = $derived.by(() => {
		try {
			return parseDate(params.date);
		} catch {
			return today(getLocalTimeZone());
		}
	});

	const request = $derived(
		adminSlotGetAll({
			facilityId,
			params: { date: selectedDate.toDate("UTC").toISOString() },
		})
	);

	const bookingsHref = $derived(resolve(`outlet/${page.params.slug}/${page.params.id}/admin/bookings`));

	const createBooking = (slotId: string, date: string) =>
		openAdminBookingCreation({
			slug: page.params.slug ?? "",
			facilityId: page.params.id ?? "",
			slotId,
			date,
			onUnavailable: () => request.refresh(),
		});

	const bookingHref = (bookingId: number) =>
		resolve("/(protected)/outlet/[slug]/[id]/admin/bookings/[bookingId]", {
			slug: page.params.slug ?? "",
			id: page.params.id ?? "",
			bookingId: String(bookingId),
		});
</script>

<div class="m-4 space-y-4">
	<div class="flex flex-wrap items-center justify-between gap-2">
		<div class="min-w-0 flex-1">
			<ScheduleDateNavigation value={selectedDate} onChange={(date) => (params.date = date)} />
		</div>
		<Button href={bookingsHref} variant="outline"><BookIcon class="size-4" />Search bookings</Button>
	</div>
	<PageBoundary>
		{#if request.loading}
			<div class="flex flex-col gap-2">
				{#each Array(6) as _, index (index)}<Skeleton class="h-25 w-full" />{/each}
			</div>
		{:else}
			<Slots
				slots={await request}
				selectedDate={selectedDate.toString()}
				isAdmin
				adminContext
				onCreateBooking={createBooking}
				onPublicBook={() => undefined}
				{bookingHref}
			/>
		{/if}
	</PageBoundary>
</div>
