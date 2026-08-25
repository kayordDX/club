<script lang="ts">
	import { Empty } from "@kayord/ui";
	import { type SlotGetAllResponse, type AdminSlotGetAllResponse } from "$lib/api";
	import { TicketXIcon } from "@lucide/svelte";
	import Slot from "./Slot.svelte";
	import AdminSlot from "./AdminSlot.svelte";

	type Props = {
		slots: SlotGetAllResponse[] | AdminSlotGetAllResponse[];
		selectedDate: string;
		refetch: () => void;
		isAdmin?: boolean;
	};

	let { slots, selectedDate, refetch, isAdmin = false }: Props = $props();

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
			<AdminSlot slot={slot as AdminSlotGetAllResponse} {selectedDate} {refetch} />
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
			<Slot {slot} {selectedDate} {refetch} />
		{/each}
	{/if}
</div>
