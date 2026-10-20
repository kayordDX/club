<script lang="ts">
	import { untrack } from "svelte";
	import type { ResolvedPathname } from "$app/types";
	import { parseDate } from "@internationalized/date";
	import { createQuery } from "@tanstack/svelte-query";
	import { BookingStatusEnum, type BookingDTO } from "$lib/api";
	import { adminBookingGetOptions, adminSlotGetAll } from "$lib/api/remote/admin.remote";
	import { buildExtras, buildPlayers } from "$lib/booking/bookingForm";
	import { formatDate, formatTime } from "$lib/booking/format";
	import { statusLabel } from "$lib/booking/status";
	import type { BookingFormSubmitValues } from "$lib/booking/schema";
	import BookingDetailsForm from "./BookingDetailsForm.svelte";
	import ScheduleDateNavigation from "$lib/components/schedule/schedule-date-navigation.svelte";
	import { Alert, Button, Card, Select } from "@kayord/ui";

	let {
		booking,
		facilityId,
		backHref,
		busy,
		onCancel,
		onSave,
	}: {
		booking: BookingDTO;
		facilityId: number;
		backHref: ResolvedPathname;
		busy: boolean;
		onCancel: () => void;
		onSave: (_values: BookingFormSubmitValues & { slotId: string; date: string }) => Promise<void>;
	} = $props();

	// A single edit session keeps the original reservation and drafts stable across refreshes.
	const original = untrack(() => booking);
	const source = original.slotContractBookings[0].slotContract.slot;
	const initialPlayers = buildPlayers(original);
	const initialExtras = buildExtras(original);
	// Legacy allowances are tied to the player's original position on the original source slot.
	// Players on any other slot must correct their contract rather than silently keep a legacy one.
	const existingContracts = original.slotContractBookings
		.filter((player) => player.slotContract.slotId === source.id)
		.map((player) => ({
			id: player.slotContractId,
			contractName: player.slotContract.contractName ?? "Existing contract",
			description: player.slotContract.description,
			price: player.slotContract.price,
		}));
	const active =
		original.bookingStatusId === BookingStatusEnum.Confirmed ||
		(original.bookingStatusId === BookingStatusEnum.Pending && new Date(original.expiresAt).getTime() > Date.now());
	let browseDate = $state(source.startDatetime.slice(0, 10));
	let destination = $state({ id: source.id, startDatetime: source.startDatetime, endDatetime: source.endDatetime });
	const date = $derived(destination.startDatetime.slice(0, 10));
	const ownPlayerCount = $derived(active && destination.id === source.id ? initialPlayers.length : 0);
	const options = createQuery(() => ({
		queryKey: ["admin-booking-options", facilityId, destination.id, original.userId],
		enabled: !!original.userId || !!original.guestName,
		queryFn: async () => {
			const result = adminBookingGetOptions({ facilityId, params: { slotId: destination.id, ...(original.userId ? { userId: original.userId } : {}) } });
			await result.refresh();
			return await result;
		},
	}));
	const slots = createQuery(() => ({
		queryKey: ["admin-slots", facilityId, browseDate],
		queryFn: async () => {
			const result = adminSlotGetAll({ facilityId, params: { date: `${browseDate}T00:00:00.000Z` } });
			await result.refresh();
			return await result;
		},
	}));
	const selectedAvailability = createQuery(() => ({
		queryKey: ["admin-slots", facilityId, date],
		queryFn: async () => {
			const result = adminSlotGetAll({ facilityId, params: { date: `${date}T00:00:00.000Z` } });
			await result.refresh();
			return await result;
		},
	}));
	const selectedSlot = $derived(selectedAvailability.data?.find((slot) => slot.id === destination.id));
	const optionsLoading = $derived(
		options.isFetching || options.isPending || selectedAvailability.isFetching || selectedAvailability.isError || !selectedSlot || options.isError
	);

	function chooseSlot(id: string) {
		if (busy) return;
		const slot = slots.data?.find((slot) => slot.id === id);
		if (!slot || !slot.isEnabled) return;
		destination = { id: slot.id, startDatetime: slot.startDatetime, endDatetime: slot.endDatetime };
	}
</script>

<Card.Root>
	<Card.Header
		><Card.Title>Destination slot</Card.Title><Card.Description
			>Keep the current slot or choose another date and slot in this facility. Player/contact drafts and extras are retained; incompatible selections must be
			corrected.</Card.Description
		></Card.Header
	>
	<Card.Content class="space-y-4">
		<fieldset disabled={busy} class="space-y-4">
			<ScheduleDateNavigation value={parseDate(browseDate)} onChange={(value) => (browseDate = value)} />
			<Select.Root type="single" value={destination.id} onValueChange={chooseSlot} disabled={slots.isFetching || slots.isError}>
				<Select.Trigger aria-label="Destination slot"
					>{formatDate(destination.startDatetime)} · {formatTime(destination.startDatetime)} - {formatTime(destination.endDatetime)}</Select.Trigger
				>
				<Select.Content>
					{#each slots.data ?? [] as slot (slot.id)}
						{@const own = active && slot.id === source.id ? initialPlayers.length : 0}
						<Select.Item value={slot.id} disabled={!slot.isEnabled || (active && slot.total - slot.booked + own < 1)}>
							{slot.resourceName ?? "Slot"} · {formatTime(slot.startDatetime)} - {formatTime(slot.endDatetime)} · {slot.total - slot.booked + own} place(s){slot.id ===
							source.id
								? " · Current slot"
								: ""}
						</Select.Item>
					{/each}
				</Select.Content>
			</Select.Root>
			{#if slots.isError || options.isError || selectedAvailability.isError}
				<Alert.Root variant="destructive"
					><Alert.Title>Destination options unavailable</Alert.Title><Alert.Description>Your drafts are retained. Retry before saving.</Alert.Description>
					<Button variant="outline" onclick={() => Promise.all([slots.refetch(), options.refetch(), selectedAvailability.refetch()])}
						>Retry destination options</Button
					>
				</Alert.Root>
			{:else if slots.data?.length === 0}<p>No slots on this date. Choose another date.</p>{/if}
		</fieldset>
	</Card.Content>
</Card.Root>

<BookingDetailsForm
	title="Edit booking details"
	description="Update players and extras or move this booking. Owner and status stay unchanged. Review destination prices before saving."
	submitLabel="Save changes"
	isSubmitting={busy}
	{backHref}
	backLabel="Back to bookings"
	{onCancel}
	showProfileShortcut={false}
	optionalContactDetails={!original.userId && !!original.guestName}
	slotId={destination.id}
	{facilityId}
	{date}
	dateLabel={formatDate(destination.startDatetime)}
	slotStartDatetime={destination.startDatetime}
	slotEndDatetime={destination.endDatetime}
	{initialPlayers}
	{initialExtras}
	availableContracts={options.data?.contracts ?? []}
	availableExtras={options.data?.extras ?? []}
	preservedContracts={destination.id === source.id ? existingContracts : []}
	authorizedAvailability
	slotAvailability={selectedSlot}
	{optionsLoading}
	{ownPlayerCount}
	enforceCapacity={active}
	reviewStatus={statusLabel(original.bookingStatusId)}
	onSubmit={(values) => onSave({ ...values, slotId: destination.id, date })}
/>
