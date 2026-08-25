<script lang="ts">
	import { Button, Card, Badge, Tooltip } from "@kayord/ui";
	import { page } from "$app/state";
	import { cn } from "@kayord/ui/utils";
	import { resolve } from "$app/paths";
	import { ChevronRightIcon, CircleDotIcon, ClockIcon, PencilIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";
	import { goto } from "$app/navigation";
	import { type AdminSlotGetAllResponse } from "$lib/api";
	import { slotAvailable } from "$lib/api/remote/slot.remote";
	import { statusBadgeVariant, statusLabel } from "$lib/booking/status";

	type Props = {
		slot: AdminSlotGetAllResponse;
		selectedDate: string;
		refetch: () => void;
	};

	let { slot, selectedDate, refetch }: Props = $props();

	const available = $derived(slot.total - slot.booked);

	const formatTime = (datetime?: string | undefined | null) => {
		if (!datetime) return "";
		return new Date(datetime).toLocaleTimeString("en-US", {
			hour: "2-digit",
			minute: "2-digit",
			hour12: false,
		});
	};

	const bookSlot = async () => {
		try {
			const isAvailable = await slotAvailable({ id: slot.id, slotCount: 1 });
			if (isAvailable) {
				const bookingUrl = resolve(`/outlet/${page.params.slug}/${page.params.id}/slot/${slot.id}?slotCount=1&date=${selectedDate}`);
				goto(bookingUrl);
			} else {
				toast.error("Not enough slots available");
				refetch();
			}
		} catch {
			console.error("Availability check failed");
		}
	};

	const manageBooking = (bookingId: number) => {
		goto(resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings/${bookingId}`));
	};
</script>

<Card.Root class={cn("w-full gap-0 p-0", !slot.isEnabled && "opacity-60")}>
	<Card.Header class="bg-muted/50 p-2">
		<div class="flex items-center justify-start gap-4">
			<div class="flex items-center gap-1">
				<ClockIcon class="text-muted-foreground size-4" />
				<div class="font-bold">
					{formatTime(slot.startDatetime)}
					<span class="text-muted-foreground">-</span>
					{formatTime(slot.endDatetime)}
				</div>
			</div>
			<div class="flex items-center gap-1">
				<CircleDotIcon class="text-muted-foreground size-4" />
				<div class="font-bold">{slot.resourceName}</div>
			</div>
		</div>
	</Card.Header>
	<Card.Content class="flex flex-wrap content-start items-center gap-2 p-2">
		{#if slot.bookings.length > 0}
			<div class="flex w-full flex-col gap-1.5">
				<div class="text-muted-foreground text-xs">Booked {slot.booked} of {slot.total}</div>
				{#each slot.bookings as booking (booking)}
					<div class="flex items-center justify-between gap-2 rounded-md border p-2">
						<div class="flex min-w-0 items-center gap-2">
							<span class="truncate font-medium">{booking.playerName ?? "—"}</span>
							<Badge variant={statusBadgeVariant(booking.bookingStatusId)}>{statusLabel(booking.bookingStatusId)}</Badge>
						</div>
						<Tooltip.Root>
							<Tooltip.Trigger>
								{#snippet child({ props })}
									<Button {...props} variant="outline" size="icon" class="size-7" onclick={() => manageBooking(booking.bookingId)}>
										<PencilIcon class="size-3.5" />
									</Button>
								{/snippet}
							</Tooltip.Trigger>
							<Tooltip.Content>Edit booking #{booking.bookingId}</Tooltip.Content>
						</Tooltip.Root>
					</div>
				{/each}
			</div>
		{/if}
		<div class={cn("flex w-full items-center justify-center py-1")}>
			{#if available > 0 && slot.isEnabled}
				<Button variant="outline" onclick={bookSlot}>
					<ChevronRightIcon /> Book
				</Button>
			{:else if available <= 0}
				<span class="text-muted-foreground text-xs">Fully booked</span>
			{:else}
				<span class="text-muted-foreground text-xs">Slot unavailable</span>
			{/if}
		</div>
	</Card.Content>
</Card.Root>
