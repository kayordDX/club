<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { toast } from "svelte-sonner";
	import { createMutation } from "@tanstack/svelte-query";
	import { BookingStatusEnum, type AdminBookingUpdateRequest } from "$lib/api";
	import { adminBookingGet, adminBookingUpdate, adminBookingUpdateStatus } from "$lib/api/remote/admin.remote";
	import { bookingGetPath } from "$lib/api/remote/booking.remote";
	import { statusLabel } from "$lib/booking/status";
	import { formatDate } from "$lib/booking/format";
	import { buildExtras, buildPlayers } from "$lib/booking/bookingForm";
	import type { BookingFormSubmitHandler } from "$lib/booking/schema";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import BookingDetails from "$lib/components/booking/booking-details.svelte";
	import BookingStatusControl from "$lib/components/booking/booking-status-control.svelte";
	import BookingDetailsForm from "$lib/components/booking/BookingDetailsForm.svelte";
	import { Alert, Button } from "@kayord/ui";
	import { BookIcon, ChevronLeftIcon, PencilIcon } from "@lucide/svelte";

	const facilityId = Number(page.params.id) || 0;
	const bookingId = Number(page.params.bookingId) || 0;
	const request = adminBookingGet({ facilityId, id: bookingId });
	const initialBooking = await request;
	const booking = $derived(request.current ?? initialBooking);
	const path = await bookingGetPath(bookingId).catch(() => undefined);
	const listHref = resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings`);
	const slot = $derived(booking.slotContractBookings?.[0]?.slotContract?.slot);
	const slotId = $derived(slot?.id ?? "");
	const ownPlayerCount = $derived(
		booking.bookingStatusId === BookingStatusEnum.Cancelled || booking.bookingStatusId === BookingStatusEnum.Expired ? 0 : booking.slotContractBookings.length
	);
	let editing = $state(false);
	let saveError = $state("");
	const update = createMutation(() => ({ mutationFn: (body: AdminBookingUpdateRequest) => adminBookingUpdate({ facilityId, id: bookingId, body }) }));
	const updateStatus = createMutation(() => ({
		mutationFn: (status: BookingStatusEnum) => adminBookingUpdateStatus({ facilityId, id: bookingId, body: { status } }),
	}));
	let refreshing = $state(false);
	const busy = $derived(update.isPending || updateStatus.isPending || refreshing);

	async function refreshBooking() {
		refreshing = true;
		try {
			await request.refresh();
		} finally {
			refreshing = false;
		}
	}

	async function changeStatus(status: BookingStatusEnum) {
		await updateStatus.mutateAsync(status);
		await refreshBooking();
		toast.success(`Status changed to ${statusLabel(status)}`);
	}

	const handleSubmit: BookingFormSubmitHandler = async ({ players, extras }) => {
		if (busy) return;
		saveError = "";
		try {
			await update.mutateAsync({
				bookings: players.map((player) => ({
					slotId,
					slotContractId: Number(player.contractId),
					name: player.name,
					cellphone: player.cellNo,
					email: player.email,
				})),
				extras: extras.map((extra) => ({ extraId: extra.id, amount: extra.amount })),
			});
			await refreshBooking();
			editing = false;
			toast.success("Booking updated");
		} catch (cause) {
			saveError = cause instanceof Error ? cause.message : "Unable to save changes. Please try again.";
		}
	};
</script>

<div class="m-4 space-y-6">
	<div class="flex flex-wrap items-start justify-between gap-4">
		<PageHeading title={editing ? "Edit booking" : "Booking"} description={`Booking #${bookingId} · Facility management`} icon={BookIcon} />
		<div class="flex flex-wrap gap-2">
			<Button href={listHref} variant="outline"><ChevronLeftIcon class="size-4" />Back to bookings</Button>
			{#if !editing}
				<Button
					disabled={busy || !slotId}
					onclick={() => {
						saveError = "";
						editing = true;
					}}><PencilIcon class="size-4" />Edit booking</Button
				>
			{/if}
		</div>
	</div>
	<BookingDetails {booking} {path} showPlayers={!editing}>
		{#snippet statusActions()}
			<BookingStatusControl status={booking.bookingStatusId as BookingStatusEnum} disabled={busy || editing} onChange={changeStatus} />
		{/snippet}
	</BookingDetails>
	{#if !slotId}
		<p class="text-muted-foreground text-sm">This booking has no slot details and cannot be edited.</p>
	{/if}
	{#if editing && slot}
		{#if saveError}
			<Alert.Root variant="destructive"><Alert.Title>Changes not saved</Alert.Title><Alert.Description>{saveError}</Alert.Description></Alert.Root>
		{/if}
		<BookingDetailsForm
			title="Edit booking details"
			description="Update players and extras, then save your changes. You can edit bookings in any status."
			submitLabel="Save changes"
			isSubmitting={busy}
			backHref={listHref}
			backLabel="Back to bookings"
			onCancel={() => (editing = false)}
			showProfileShortcut={false}
			{slotId}
			{facilityId}
			date={slot.startDatetime.slice(0, 10)}
			dateLabel={formatDate(slot.startDatetime)}
			slotStartDatetime={slot.startDatetime}
			slotEndDatetime={slot.endDatetime}
			initialPlayers={buildPlayers(booking)}
			initialExtras={buildExtras(booking)}
			{ownPlayerCount}
			onSubmit={handleSubmit}
		/>
	{/if}
</div>
