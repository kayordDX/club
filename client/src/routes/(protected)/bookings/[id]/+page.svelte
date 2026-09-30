<script lang="ts">
	import { BookIcon, PencilIcon } from "@lucide/svelte";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import BookingDetails from "$lib/components/booking/booking-details.svelte";
	import { BookingStatusEnum, type BookingPathDTO } from "$lib/api";
	import { bookingGet, bookingGetPath } from "$lib/api/remote/booking.remote";
	import { Button } from "@kayord/ui";

	const bookingId = Number(page.params.id) || 0;
	const booking = await bookingGet(bookingId);
	// Older cancelled bookings may no longer have slot data.
	const path: BookingPathDTO | undefined = await bookingGetPath(bookingId).catch(() => undefined);
	const editHref = resolve(`/bookings/${bookingId}/edit`);
	const canEdit = booking.bookingStatusId === BookingStatusEnum.Pending;
</script>

<div class="m-4 space-y-6">
	<div class="flex flex-wrap items-start justify-between gap-4">
		<PageHeading title="Booking" description={`Booking #${bookingId}`} icon={BookIcon} />
		{#if canEdit}
			<Button href={editHref}><PencilIcon class="size-4" />Edit booking</Button>
		{/if}
	</div>
	<BookingDetails {booking} {path} />
</div>
