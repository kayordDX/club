<script lang="ts">
	import { BookIcon, PencilIcon } from "@lucide/svelte";
	import { resolve } from "$app/paths";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import BookingPlayers from "$lib/components/BookingPlayers.svelte";
	import BookingExtras from "$lib/components/BookingExtras.svelte";
	import { formatCurrency, formatDate, formatDateTime, formatTime } from "$lib/booking/format";
	import { BookingStatusEnum, type BookingPathDTO } from "$lib/api";
	import { bookingGet, bookingGetPath } from "$lib/api/remote/booking.remote";
	import { page } from "$app/state";
	import { Button, Card } from "@kayord/ui";

	const bookingId = Number(page.params.id) || 0;
	const booking = await bookingGet(bookingId);
	// Cancelled bookings created before players were retained on cancel have no slot data,
	// so the path lookup 404s — degrade to "—" instead of failing the whole page.
	const path: BookingPathDTO | undefined = await bookingGetPath(bookingId).catch(() => undefined);
	const editHref = resolve(`/bookings/${bookingId}/edit`);
	const canEdit = booking.bookingStatus?.id === BookingStatusEnum.Pending;
</script>

<div class="m-4">
	<div class="flex items-start justify-between gap-4">
		<PageHeading title="Booking" description="Booking details" icon={BookIcon} />

		{#if canEdit}
			<Button href={editHref}>
				<PencilIcon class="size-4" />
				Edit booking
			</Button>
		{/if}
	</div>

	<div class="mt-8 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
		<Card.Root>
			<Card.Header>
				<Card.Title>Booking</Card.Title>
			</Card.Header>
			<Card.Content class="space-y-3">
				<div>
					<div class="text-muted-foreground text-sm">Booking ID</div>
					<div>{booking.id ?? "—"}</div>
				</div>

				<div>
					<div class="text-muted-foreground text-sm">Date</div>
					<div>{formatDate(booking.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}</div>
				</div>

				<div>
					<div class="text-muted-foreground text-sm">Time</div>
					<div>{formatTime(booking.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}</div>
				</div>

				<div>
					<div class="text-muted-foreground text-sm">Status</div>
					<div>{booking.bookingStatus?.name ?? "—"}</div>
				</div>

				<div>
					<div class="text-muted-foreground text-sm">Status updated</div>
					<div>{formatDateTime(booking.bookingStatusDate)}</div>
				</div>

				{#if booking.bookingStatus?.id === 1}
					<div>
						<div class="text-muted-foreground text-sm">Expires at</div>
						<div>{formatDateTime(booking.expiresAt)}</div>
					</div>
				{/if}
			</Card.Content>
		</Card.Root>

		<Card.Root>
			<Card.Header>
				<Card.Title>Facility</Card.Title>
			</Card.Header>
			<Card.Content class="space-y-3">
				<div>
					<div class="text-muted-foreground text-sm">Outlet</div>
					<div>{path?.outletName ?? "—"}</div>
				</div>
				<div>
					<div class="text-muted-foreground text-sm">Facility</div>
					<div>{path?.facilityName ?? "—"}</div>
				</div>
			</Card.Content>
		</Card.Root>

		<Card.Root>
			<Card.Header>
				<Card.Title>Payment</Card.Title>
			</Card.Header>
			<Card.Content class="space-y-3">
				<div>
					<div class="text-muted-foreground text-sm">Paid</div>
					<div>{booking.isPaid ? "Yes" : "No"}</div>
				</div>
				<div>
					<div class="text-muted-foreground text-sm">Amount paid</div>
					<div>{formatCurrency(booking.amountPaid)}</div>
				</div>
				<div>
					<div class="text-muted-foreground text-sm">Amount outstanding</div>
					<div>{formatCurrency(booking.amountOutstanding)}</div>
				</div>
			</Card.Content>
		</Card.Root>

		<Card.Root>
			<Card.Header>
				<Card.Title>Booked by</Card.Title>
			</Card.Header>
			<Card.Content class="space-y-3">
				<div>
					<div class="text-muted-foreground text-sm">Name</div>
					<div>{booking.user ? `${booking.user.firstName} ${booking.user.lastName}` : "—"}</div>
				</div>
			</Card.Content>
		</Card.Root>

		<Card.Root class="md:col-span-2 xl:col-span-3">
			<Card.Header>
				<Card.Title>Slot contract bookings</Card.Title>
			</Card.Header>
			<Card.Content>
				{#if booking.slotContractBookings?.length}
					<BookingPlayers players={booking.slotContractBookings} />
				{:else}
					<div class="text-muted-foreground">No slot contract bookings.</div>
				{/if}
			</Card.Content>
		</Card.Root>

		<Card.Root class="md:col-span-2 xl:col-span-3">
			<Card.Header>
				<Card.Title>Extras</Card.Title>
			</Card.Header>
			<Card.Content>
				{#if booking.extraBookings?.length}
					<BookingExtras extras={booking.extraBookings} />
				{:else}
					<div class="text-muted-foreground">No extras booked.</div>
				{/if}
			</Card.Content>
		</Card.Root>
	</div>
</div>
