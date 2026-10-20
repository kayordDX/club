<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { toast } from "svelte-sonner";
	import { createMutation, useQueryClient } from "@tanstack/svelte-query";
	import { BookingStatusEnum, type AdminBookingUpdateRequest } from "$lib/api";
	import { adminBookingGet, adminBookingGetPath, adminBookingUpdate, adminBookingUpdateStatus, adminSlotGetAll } from "$lib/api/remote/admin.remote";
	import { slotGetAll } from "$lib/api/remote/slot.remote";
	import { statusLabel } from "$lib/booking/status";
	import { getBookingPayUrl } from "$lib/booking/payUrl";
	import type { BookingFormSubmitValues } from "$lib/booking/schema";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import BookingDetails from "$lib/components/booking/booking-details.svelte";
	import BookingStatusControl from "$lib/components/booking/booking-status-control.svelte";
	import AdminBookingEditor from "$lib/components/booking/admin-booking-editor.svelte";
	import AdminBookingPayments from "$lib/components/admin-booking-payments.svelte";
	import { Alert, Button } from "@kayord/ui";
	import { BookIcon, ChevronLeftIcon, PencilIcon } from "@lucide/svelte";

	const facilityId = Number(page.params.id) || 0;
	const bookingId = Number(page.params.bookingId) || 0;
	const request = adminBookingGet({ facilityId, id: bookingId });
	const initialBooking = await request;
	const booking = $derived(request.current ?? initialBooking);
	const pathRequest = adminBookingGetPath({ facilityId, id: bookingId });
	const initialPath = await pathRequest.catch(() => undefined);
	const path = $derived(pathRequest.current ?? initialPath);
	const listHref = resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings`);
	const returnUrl = resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings/${bookingId}`);
	const customerPayUrl = $derived(path && booking.userId ? getBookingPayUrl(bookingId, path, booking.slotContractBookings?.length ?? 0) : undefined);
	const slot = $derived(booking.slotContractBookings?.[0]?.slotContract?.slot);
	const slotId = $derived(slot?.id ?? "");
	let editing = $state(false);
	let saveError = $state("");
	const update = createMutation(() => ({ mutationFn: (body: AdminBookingUpdateRequest) => adminBookingUpdate({ facilityId, id: bookingId, body }) }));
	const updateStatus = createMutation(() => ({
		mutationFn: (status: BookingStatusEnum) => adminBookingUpdateStatus({ facilityId, id: bookingId, body: { status } }),
	}));
	const queryClient = useQueryClient();
	let refreshing = $state(false);
	let refreshError = $state("");
	let paymentBusy = $state(false);
	const busy = $derived(update.isPending || updateStatus.isPending || paymentBusy || refreshing || !!refreshError);
	let affectedDates: string[] = [];

	async function refreshBooking(refreshPayments = true) {
		refreshing = true;
		try {
			const dates = [...new Set([...affectedDates, ...(slot ? [slot.startDatetime.slice(0, 10)] : [])])];
			const results = await Promise.allSettled([
				request.refresh(),
				pathRequest.refresh(),
				queryClient.invalidateQueries({ queryKey: ["admin-bookings", facilityId] }),
				queryClient.invalidateQueries({ queryKey: ["admin-slots", facilityId] }),
				queryClient.invalidateQueries({ queryKey: ["admin-booking-options", facilityId] }),
				...(refreshPayments ? [queryClient.invalidateQueries({ queryKey: ["admin-booking-payments", facilityId, bookingId] })] : []),
				...dates.flatMap((day) => {
					const date = `${day}T00:00:00.000Z`;
					return [adminSlotGetAll({ facilityId, params: { date } }).refresh(), slotGetAll({ facilityId, date }).refresh()];
				}),
			]);
			if (results.some((result) => result.status === "rejected")) throw new Error("Refresh failed");
			refreshError = "";
			affectedDates = [];
		} catch {
			refreshError = "Changes were saved, but current booking details or availability could not be refreshed. Refresh before making another change.";
			throw new Error(refreshError);
		} finally {
			refreshing = false;
		}
	}

	async function changeStatus(status: BookingStatusEnum) {
		await updateStatus.mutateAsync(status);
		await refreshBooking();
		toast.success(`Status changed to ${statusLabel(status)}`);
	}

	const handleSubmit = async ({ players, extras, slotId: destinationSlotId, date }: BookingFormSubmitValues & { slotId: string; date: string }) => {
		if (busy) return;
		saveError = "";
		try {
			await update.mutateAsync({
				bookings: players.map((player) => ({
					slotId: destinationSlotId,
					slotContractId: Number(player.contractId),
					name: player.name,
					cellphone: player.cellNo,
					email: player.email,
				})),
				extras: extras.map((extra) => ({ extraId: extra.id, amount: extra.amount })),
			});
			affectedDates = [...new Set([...(slot ? [slot.startDatetime.slice(0, 10)] : []), date])];
			editing = false;
			toast.success("Booking updated");
			await refreshBooking();
		} catch (cause) {
			saveError = cause instanceof Error ? cause.message : "Unable to save changes. Please try again.";
		}
	};
</script>

<div class="m-4 space-y-6">
	<div class="flex flex-wrap items-start justify-between gap-4">
		<PageHeading title={editing ? "Edit booking" : "Booking"} description={`Booking #${bookingId} · Facility management`} icon={BookIcon} />
		<div class="flex flex-wrap gap-2">
			<Button href={listHref} disabled={update.isPending || paymentBusy || refreshing} variant="outline"
				><ChevronLeftIcon class="size-4" />Back to bookings</Button
			>
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
	{#if refreshError}
		<Alert.Root variant="destructive">
			<Alert.Title>Refresh required</Alert.Title>
			<Alert.Description>{refreshError}</Alert.Description>
			<Button disabled={refreshing} onclick={() => refreshBooking().catch(() => undefined)}>Refresh booking</Button>
		</Alert.Root>
	{/if}
	<BookingDetails {booking} {path} showPlayers={!editing}>
		{#snippet statusActions()}
			<BookingStatusControl status={booking.bookingStatusId as BookingStatusEnum} disabled={busy || editing} onChange={changeStatus} />
		{/snippet}
	</BookingDetails>
	<AdminBookingPayments
		{facilityId}
		{bookingId}
		{booking}
		{returnUrl}
		{customerPayUrl}
		disabled={busy || editing}
		onrefresh={() => refreshBooking(false)}
		onbusy={(value) => (paymentBusy = value)}
	/>
	{#if !slotId}
		<p class="text-muted-foreground text-sm">This booking has no slot details and cannot be edited.</p>
	{/if}
	{#if editing && slot}
		{#if saveError}
			<Alert.Root variant="destructive"><Alert.Title>Changes not saved</Alert.Title><Alert.Description>{saveError}</Alert.Description></Alert.Root>
		{/if}
		<AdminBookingEditor {booking} {facilityId} backHref={listHref} {busy} onCancel={() => (editing = false)} onSave={handleSubmit} />
	{/if}
</div>
