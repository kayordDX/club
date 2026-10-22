<script lang="ts">
	import { Button, Empty } from "@kayord/ui";
	import type { ResolvedPathname } from "$app/types";
	import { type SlotGetAllResponse, type AdminSlotGetAllResponse } from "$lib/api";
	import { SlidersHorizontalIcon, TicketXIcon } from "@lucide/svelte";
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
	let selectedResource = $state<string | null>(null);

	const resourceName = (slot: SlotGetAllResponse | AdminSlotGetAllResponse) => slot.resourceName?.trim() || "Other";
	const resources = $derived([...new Set(slots.map(resourceName))].sort((a, b) => a.localeCompare(b, undefined, { numeric: true })));
	// A different day or facility may no longer offer the selected resource.
	const activeResource = $derived(selectedResource !== null && resources.includes(selectedResource) ? selectedResource : null);
	const filteredSlots = $derived(activeResource === null ? slots : slots.filter((slot) => resourceName(slot) === activeResource));
	const availableSlots = $derived(filteredSlots.filter((slot) => slot.isAvailable && slot.isEnabled && slot.total > slot.booked));
</script>

<div class="space-y-4">
	{#if resources.length > 1}
		<div class="bg-muted/30 space-y-3 rounded-xl border p-4">
			<div class="flex items-center gap-2 text-sm font-medium">
				<SlidersHorizontalIcon class="text-muted-foreground size-4" />
				Tee, court or lane
			</div>
			<div class="flex flex-wrap gap-2" role="group" aria-label="Filter by tee, court or lane">
				<Button
					size="sm"
					variant={activeResource === null ? "default" : "outline"}
					aria-pressed={activeResource === null}
					onclick={() => (selectedResource = null)}
				>
					All
				</Button>
				{#each resources as resource (resource)}
					<Button
						size="sm"
						variant={activeResource === resource ? "default" : "outline"}
						aria-pressed={activeResource === resource}
						onclick={() => (selectedResource = resource)}
					>
						{resource}
					</Button>
				{/each}
			</div>
		</div>
	{/if}

	<p class="text-muted-foreground text-sm" role="status">
		{filteredSlots.length}
		{filteredSlots.length === 1 ? "time slot" : "time slots"}{activeResource === null ? "" : ` · ${activeResource}`}
		{` · ${availableSlots.length} available`}
	</p>

	{#if filteredSlots.length === 0 || (!isAdmin && availableSlots.length === 0)}
		<Empty.Root class="rounded-xl border border-dashed">
			<Empty.Header>
				<Empty.Media variant="icon"><TicketXIcon /></Empty.Media>
				<Empty.Title>No Slots Available</Empty.Title>
				<Empty.Description>Try another date{resources.length > 1 ? " or choose a different tee, court or lane" : ""}.</Empty.Description>
			</Empty.Header>
		</Empty.Root>
	{/if}

	<div class="slot-grid">
		{#each filteredSlots as slot (slot.id)}
			{#if isAdmin}
				<AdminSlot slot={slot as AdminSlotGetAllResponse} {selectedDate} {adminContext} {onCreateBooking} {bookingHref} />
			{:else}
				<Slot {slot} {selectedDate} onBook={onPublicBook} />
			{/if}
		{/each}
	</div>
</div>

<style>
	.slot-grid {
		display: grid;
		grid-template-columns: repeat(auto-fill, minmax(min(100%, 22rem), 1fr));
		align-items: start;
		gap: 1rem;
	}
</style>
