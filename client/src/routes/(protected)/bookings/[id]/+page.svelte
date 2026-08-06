<script lang="ts">
	import { BookIcon, PencilIcon } from "@lucide/svelte";
	import { resolve } from "$app/paths";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import BookingPlayers from "$lib/components/BookingPlayers.svelte";
	import BookingExtras from "$lib/components/BookingExtras.svelte";
	import Await from "$lib/components/Await.svelte";
	import { formatCurrency, formatDate, formatDateTime, formatTime } from "$lib/booking/format";
	import { BookingStatusEnum } from "$lib/api";
	import { bookingGet, bookingGetPath } from "$lib/api/remote/booking.remote";
	import { page } from "$app/state";
	import { Button, Card } from "@kayord/ui";

	const bookingId = Number(page.params.id) || 0;
	const booking = bookingGet(bookingId);
	const path = bookingGetPath(bookingId);
	const editHref = resolve(`/bookings/${bookingId}/edit`);
</script>

<div class="m-4">
	<Await promise={booking}>
		{#snippet children(b)}
			<div class="flex items-start justify-between gap-4">
				<PageHeading title="Booking" description="Booking details" icon={BookIcon} />

				{#if b.bookingStatus?.id === BookingStatusEnum.Pending}
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
							<div>{b.id ?? "—"}</div>
						</div>

						<div>
							<div class="text-muted-foreground text-sm">Date</div>
							<div>{formatDate(b.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}</div>
						</div>

						<div>
							<div class="text-muted-foreground text-sm">Time</div>
							<div>{formatTime(b.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}</div>
						</div>

						<div>
							<div class="text-muted-foreground text-sm">Status</div>
							<div>{b.bookingStatus?.name ?? "—"}</div>
						</div>

						<div>
							<div class="text-muted-foreground text-sm">Status updated</div>
							<div>{formatDateTime(b.bookingStatusDate)}</div>
						</div>

						{#if b.bookingStatus?.id === 1}
							<div>
								<div class="text-muted-foreground text-sm">Expires at</div>
								<div>{formatDateTime(b.expiresAt)}</div>
							</div>
						{/if}
					</Card.Content>
				</Card.Root>

				<Card.Root>
					<Card.Header>
						<Card.Title>Facility</Card.Title>
					</Card.Header>
					<Card.Content class="space-y-3">
						<Await promise={path}>
							{#snippet children(p)}
								<div>
									<div class="text-muted-foreground text-sm">Outlet</div>
									<div>{p?.outletName ?? "—"}</div>
								</div>
								<div>
									<div class="text-muted-foreground text-sm">Facility</div>
									<div>{p?.facilityName ?? "—"}</div>
								</div>
							{/snippet}
						</Await>
					</Card.Content>
				</Card.Root>

				<Card.Root>
					<Card.Header>
						<Card.Title>Payment</Card.Title>
					</Card.Header>
					<Card.Content class="space-y-3">
						<div>
							<div class="text-muted-foreground text-sm">Paid</div>
							<div>{b.isPaid ? "Yes" : "No"}</div>
						</div>
						<div>
							<div class="text-muted-foreground text-sm">Amount paid</div>
							<div>{formatCurrency(b.amountPaid)}</div>
						</div>
						<div>
							<div class="text-muted-foreground text-sm">Amount outstanding</div>
							<div>{formatCurrency(b.amountOutstanding)}</div>
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
							<div>{b.user ? `${b.user.firstName} ${b.user.lastName}` : "—"}</div>
						</div>
					</Card.Content>
				</Card.Root>

				<Card.Root class="md:col-span-2 xl:col-span-3">
					<Card.Header>
						<Card.Title>Slot contract bookings</Card.Title>
					</Card.Header>
					<Card.Content>
						{#if b.slotContractBookings?.length}
							<BookingPlayers players={b.slotContractBookings} />
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
						{#if b.extraBookings?.length}
							<BookingExtras extras={b.extraBookings} />
						{:else}
							<div class="text-muted-foreground">No extras booked.</div>
						{/if}
					</Card.Content>
				</Card.Root>
			</div>
		{/snippet}
	</Await>
</div>
