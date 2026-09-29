<script lang="ts">
	import type { Snippet } from "svelte";
	import { BookingStatusEnum, type BookingDTO, type BookingPathDTO } from "$lib/api";
	import { formatCurrency, formatDate, formatDateTime, formatTime } from "$lib/booking/format";
	import { statusBadgeVariant, statusLabel } from "$lib/booking/status";
	import BookingPlayers from "$lib/components/BookingPlayers.svelte";
	import BookingExtras from "$lib/components/BookingExtras.svelte";
	import { Badge, Card } from "@kayord/ui";

	let {
		booking,
		path,
		statusActions,
		showPlayers = true,
	}: {
		booking: BookingDTO;
		path?: BookingPathDTO;
		statusActions?: Snippet;
		showPlayers?: boolean;
	} = $props();
	const slot = $derived(booking.slotContractBookings?.[0]?.slotContract?.slot);
</script>

<div class="grid gap-4 lg:grid-cols-3">
	<Card.Root>
		<Card.Header><Card.Title>Booking #{booking.id}</Card.Title></Card.Header>
		<Card.Content class="space-y-4">
			<div class="flex flex-wrap items-center gap-2">
				<Badge variant={statusBadgeVariant(booking.bookingStatusId)}>{statusLabel(booking.bookingStatusId)}</Badge>
				{@render statusActions?.()}
			</div>
			<dl class="space-y-3 text-sm">
				<div>
					<dt class="text-muted-foreground">Date</dt>
					<dd class="font-medium">{formatDate(slot?.startDatetime)}</dd>
				</div>
				<div>
					<dt class="text-muted-foreground">Time</dt>
					<dd>{formatTime(slot?.startDatetime)} – {formatTime(slot?.endDatetime)}</dd>
				</div>
				<div>
					<dt class="text-muted-foreground">Status updated</dt>
					<dd>{formatDateTime(booking.bookingStatusDate)}</dd>
				</div>
				{#if booking.bookingStatusId === BookingStatusEnum.Pending}
					<div>
						<dt class="text-muted-foreground">Expires at</dt>
						<dd>{formatDateTime(booking.expiresAt)}</dd>
					</div>
				{/if}
			</dl>
		</Card.Content>
	</Card.Root>
	<Card.Root>
		<Card.Header><Card.Title>Booking information</Card.Title></Card.Header>
		<Card.Content>
			<dl class="space-y-3 text-sm">
				<div>
					<dt class="text-muted-foreground">Outlet</dt>
					<dd>{path?.outletName ?? "—"}</dd>
				</div>
				<div>
					<dt class="text-muted-foreground">Facility</dt>
					<dd>{path?.facilityName ?? "—"}</dd>
				</div>
				<div>
					<dt class="text-muted-foreground">Booked by</dt>
					<dd>{booking.user ? `${booking.user.firstName} ${booking.user.lastName}` : "—"}</dd>
				</div>
			</dl>
		</Card.Content>
	</Card.Root>
	<Card.Root>
		<Card.Header><Card.Title>Payment</Card.Title></Card.Header>
		<Card.Content class="space-y-4">
			<Badge variant={booking.isPaid ? "default" : "outline"}>{booking.isPaid ? "Paid" : "Unpaid"}</Badge>
			<dl class="space-y-3 text-sm">
				<div>
					<dt class="text-muted-foreground">Amount paid</dt>
					<dd>{formatCurrency(booking.amountPaid)}</dd>
				</div>
				<div>
					<dt class="text-muted-foreground">Amount outstanding</dt>
					<dd class="text-lg font-semibold">{formatCurrency(booking.amountOutstanding)}</dd>
				</div>
			</dl>
		</Card.Content>
	</Card.Root>
	{#if showPlayers}
		<Card.Root class="lg:col-span-3">
			<Card.Header><Card.Title>Players</Card.Title></Card.Header>
			<Card.Content>
				{#if booking.slotContractBookings?.length}<BookingPlayers players={booking.slotContractBookings} />
				{:else}<p class="text-muted-foreground text-sm">No players booked.</p>{/if}
			</Card.Content>
		</Card.Root>
		<Card.Root class="lg:col-span-3">
			<Card.Header><Card.Title>Extras</Card.Title></Card.Header>
			<Card.Content>
				{#if booking.extraBookings?.length}<BookingExtras extras={booking.extraBookings} />
				{:else}<p class="text-muted-foreground text-sm">No extras booked.</p>{/if}
			</Card.Content>
		</Card.Root>
	{/if}
</div>
