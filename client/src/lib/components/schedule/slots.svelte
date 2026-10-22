<script lang="ts">
	import { Button, Empty, Select, Separator } from "@kayord/ui";
	import type { ResolvedPathname } from "$app/types";
	import { type SlotGetAllResponse, type AdminSlotGetAllResponse } from "#lib/api/index.js";
	import { FunnelIcon, TicketXIcon } from "@lucide/svelte";
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
	let minimumAvailable = $state("0");

	const maxCapacity = $derived(slots.reduce((max, slot) => Math.max(max, slot.total), 1));
	const availabilityLabel = (minimum: number) => (minimum === 1 ? "Available only" : `${minimum}+ spaces available`);
	const isBookable = (slot: SlotGetAllResponse | AdminSlotGetAllResponse) => slot.isAvailable && slot.isEnabled && slot.total > slot.booked;

	const resourceName = (slot: SlotGetAllResponse | AdminSlotGetAllResponse) => slot.resourceName?.trim() || "Other";
	const resources = $derived([...new Set(slots.map(resourceName))].sort((a, b) => a.localeCompare(b, undefined, { numeric: true })));
	// A different day or facility may no longer offer the selected resource.
	const activeResource = $derived(selectedResource !== null && resources.includes(selectedResource) ? selectedResource : null);
	const filteredSlots = $derived(
		slots.filter(
			(slot) =>
				(activeResource === null || resourceName(slot) === activeResource) &&
				(Number(minimumAvailable) === 0 || (isBookable(slot) && slot.total - slot.booked >= Number(minimumAvailable)))
		)
	);
	const availableSlots = $derived(filteredSlots.filter(isBookable));
</script>

<div class="space-y-4">
	<div class="bg-muted/30 flex flex-wrap items-center gap-3 rounded-xl border p-2">
		<FunnelIcon class="text-muted-foreground size-4" />
		<div class="h-5">
			<Separator orientation="vertical" />
		</div>
		{#if resources.length > 1}
			<div class="flex flex-wrap items-center gap-2" role="group" aria-label="Filter by tee, court or lane">
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
			<div class="h-5">
				<Separator orientation="vertical" />
			</div>
		{/if}

		<Select.Root type="single" bind:value={minimumAvailable}>
			<Select.Trigger aria-label="Filter by availability">
				{minimumAvailable === "0" ? "All slots" : availabilityLabel(Number(minimumAvailable))}
			</Select.Trigger>
			<Select.Content>
				<Select.Item value="0" label="All slots" />
				{#each Array.from({ length: maxCapacity }, (_, i) => i + 1) as minimum (minimum)}
					<Select.Item value={String(minimum)} label={availabilityLabel(minimum)} />
				{/each}
			</Select.Content>
		</Select.Root>
	</div>

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
				<Empty.Description>Try another date or adjust the filters.</Empty.Description>
			</Empty.Header>
		</Empty.Root>
	{/if}

	<div class="grid grid-cols-[repeat(auto-fill,minmax(min(100%,22rem),1fr))] items-start gap-4">
		{#each filteredSlots as slot (slot.id)}
			{#if isAdmin}
				<AdminSlot slot={slot as AdminSlotGetAllResponse} {selectedDate} {adminContext} {onCreateBooking} {bookingHref} />
			{:else}
				<Slot {slot} {selectedDate} onBook={onPublicBook} />
			{/if}
		{/each}
	</div>
</div>
