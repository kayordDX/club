<script lang="ts">
	import { Button, Card, Badge, StatusDot } from "@kayord/ui";
	import type { ResolvedPathname } from "$app/types";
	import { CircleDotIcon, CirclePlusIcon, ClockIcon, PencilIcon } from "@lucide/svelte";
	import { type AdminSlotGetAllResponse } from "$lib/api";
	import { statusBadgeVariant, statusLabel } from "$lib/booking/status";

	type SlotCreationAction = (..._args: [string, string]) => void;
	type Props = {
		slot: AdminSlotGetAllResponse;
		selectedDate: string;
		adminContext?: boolean;
		onCreateBooking: SlotCreationAction;
		bookingHref: (..._args: [number]) => ResolvedPathname;
	};

	let { slot, selectedDate, adminContext = false, onCreateBooking, bookingHref }: Props = $props();

	const available = $derived(slot.total - slot.booked);

	const formatTime = (datetime?: string | undefined | null) => {
		if (!datetime) return "";
		return new Date(datetime).toLocaleTimeString("en-US", {
			hour: "2-digit",
			minute: "2-digit",
			hour12: false,
		});
	};
</script>

<Card.Root class={["w-full min-w-0 gap-0 p-0", !slot.isEnabled && "opacity-60"]}>
	<Card.Header class="bg-muted/40 border-b p-4">
		<div class="flex flex-wrap items-center justify-between gap-2">
			<div class="flex items-center gap-1">
				<ClockIcon class="text-muted-foreground size-4" />
				<div class="text-base font-semibold tabular-nums">
					{formatTime(slot.startDatetime)}
					<span class="text-muted-foreground">-</span>
					{formatTime(slot.endDatetime)}
				</div>
			</div>
			<div class="bg-background flex items-center gap-1.5 rounded-md border px-2 py-1 text-xs">
				<CircleDotIcon class="text-muted-foreground size-4" />
				<div class="font-bold">{slot.resourceName}</div>
			</div>
		</div>
	</Card.Header>
	<Card.Content class="flex flex-wrap content-start items-center gap-4 p-4">
		<div class="flex w-full flex-col gap-1.5">
			{#if !slot.isEnabled}
				<p class="text-muted-foreground">Slot unavailable</p>
			{:else if available === 0}
				<p class="text-muted-foreground">Fully booked</p>
			{/if}
			<p class="text-muted-foreground">Booked {slot.booked} of {slot.total} · {available} available</p>
			<div class="flex grow flex-row items-center gap-1">
				{#each Array(available), i (i)}
					<StatusDot.Root variant="success" />
				{/each}
				{#each Array(slot.booked), i (i)}
					<StatusDot.Root variant="error" />
				{/each}
			</div>
			{#each slot.bookings as booking (booking.bookingId)}
				<div class="flex items-center justify-between gap-2 rounded-md border p-2">
					<div class="flex min-w-0 items-center gap-2">
						<span class="truncate font-medium">{booking.playerName ?? "—"}</span>
						<Badge variant={statusBadgeVariant(booking.bookingStatusId)}>{statusLabel(booking.bookingStatusId)}</Badge>
					</div>
					<Button variant="outline" size="icon" class="size-7" aria-label={`Manage booking #${booking.bookingId}`} href={bookingHref(booking.bookingId)}>
						<PencilIcon class="size-3.5" />
					</Button>
				</div>
			{/each}
		</div>
		<div class="flex w-full items-center justify-end">
			{#if available > 0 && slot.isEnabled}
				<Button variant="outline" onclick={() => onCreateBooking(slot.id, selectedDate)}>
					<CirclePlusIcon />
					{adminContext ? "Create booking" : "Book"}
				</Button>
			{/if}
		</div>
	</Card.Content>
</Card.Root>
