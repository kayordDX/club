<script lang="ts">
	import { Empty } from "@kayord/ui";
	import type { ResolvedPathname } from "$app/types";
	import { type SlotGetAllResponse, type AdminSlotGetAllResponse } from "$lib/api";
	import { TicketXIcon } from "@lucide/svelte";
	import Slot from "./public-slot.svelte";
	import AdminSlot from "./admin-slot.svelte";

	type SlotCreationAction = (..._args: [string, string]) => void;
	type Props = {
		slots: SlotGetAllResponse[] | AdminSlotGetAllResponse[];
		selectedDate: string;
		isAdmin?: boolean;
		adminContext?: boolean;
		onCreateBooking: SlotCreationAction;
		onPublicBook: (..._args: [string, number, string]) => void;
		bookingHref: (..._args: [number]) => ResolvedPathname;
	};

	let { slots, selectedDate, isAdmin = false, adminContext = false, onCreateBooking, onPublicBook, bookingHref }: Props = $props();

	const availableSlots = $derived(slots.filter((slot) => slot.isAvailable));
</script>

<div class="grid grid-cols-1 place-items-center gap-2">
	{#if slots.length == 0}
		<Empty.Root>
			<Empty.Header>
				<Empty.Media variant="icon">
					<TicketXIcon />
				</Empty.Media>
				<Empty.Title>No Slots Available</Empty.Title>
				<Empty.Description>There are no slots available for your current selection</Empty.Description>
			</Empty.Header>
			<Empty.Content></Empty.Content>
		</Empty.Root>
	{:else if isAdmin}
		{#each slots as slot (slot.id)}
			<AdminSlot slot={slot as AdminSlotGetAllResponse} {selectedDate} {adminContext} {onCreateBooking} {bookingHref} />
		{/each}
	{:else}
		{#if availableSlots.length == 0}
			<Empty.Root>
				<Empty.Header>
					<Empty.Media variant="icon">
						<TicketXIcon />
					</Empty.Media>
					<Empty.Title>No Slots Available</Empty.Title>
					<Empty.Description>There are no slots available for your current selection</Empty.Description>
				</Empty.Header>
				<Empty.Content></Empty.Content>
			</Empty.Root>
		{/if}
		{#each slots as slot (slot.id)}
			<Slot {slot} {selectedDate} onBook={onPublicBook} />
		{/each}
	{/if}
</div>
