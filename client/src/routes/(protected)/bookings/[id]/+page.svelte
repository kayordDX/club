<script lang="ts">
	import { BookIcon, PencilIcon } from "@lucide/svelte";
	import { resolve } from "$app/paths";
	import PageHeading from "../../settings/PageHeading.svelte";
	import Query from "$lib/components/Query.svelte";
	import BookingPlayers from "$lib/components/BookingPlayers.svelte";
	import BookingExtras from "$lib/components/BookingExtras.svelte";
	import { formatCurrency, formatDate, formatDateTime, formatTime } from "$lib/booking/format";
	import { BookingStatusEnum, createBookingGet, createBookingGetPath } from "$lib/api";
	import { page } from "$app/state";
	import { Button, Card } from "@kayord/ui";

	const bookingId = $derived(Number(page.params.id) || 0);
	const query = createBookingGet(() => bookingId);
	const pathQuery = createBookingGetPath(
		() => bookingId,
		() => ({ query: { enabled: bookingId > 0 } })
	);
	const path = $derived(pathQuery.data);
	const canEdit = $derived(query.data?.bookingStatus?.id === BookingStatusEnum.Pending);
	const editHref = $derived(resolve(`/bookings/${bookingId}/edit`));
</script>

<div class="m-4">
	<Query {query} emptyText="Booking not found">
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
						<div>{query.data?.id ?? "—"}</div>
					</div>

					<div>
						<div class="text-muted-foreground text-sm">Date</div>
						<div>{formatDate(query.data?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}</div>
					</div>

					<div>
						<div class="text-muted-foreground text-sm">Time</div>
						<div>{formatTime(query.data?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}</div>
					</div>

					<div>
						<div class="text-muted-foreground text-sm">Status</div>
						<div>{query.data?.bookingStatus?.name ?? "—"}</div>
					</div>

					<div>
						<div class="text-muted-foreground text-sm">Status updated</div>
						<div>{formatDateTime(query.data?.bookingStatusDate)}</div>
					</div>

					{#if query.data?.bookingStatus?.id === 1}
						<div>
							<div class="text-muted-foreground text-sm">Expires at</div>
							<div>{formatDateTime(query.data?.expiresAt)}</div>
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
						<div>{query.data?.isPaid ? "Yes" : "No"}</div>
					</div>

					<div>
						<div class="text-muted-foreground text-sm">Amount paid</div>
						<div>{formatCurrency(query.data?.amountPaid)}</div>
					</div>

					<div>
						<div class="text-muted-foreground text-sm">Amount outstanding</div>
						<div>{formatCurrency(query.data?.amountOutstanding)}</div>
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
						<div>
							{query.data?.user ? `${query.data.user.firstName} ${query.data.user.lastName}` : "—"}
						</div>
					</div>
				</Card.Content>
			</Card.Root>

			<Card.Root class="md:col-span-2 xl:col-span-3">
				<Card.Header>
					<Card.Title>Slot contract bookings</Card.Title>
				</Card.Header>
				<Card.Content>
					{#if query.data?.slotContractBookings?.length}
						<BookingPlayers players={query.data.slotContractBookings} />
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
					{#if query.data?.extraBookings?.length}
						<BookingExtras extras={query.data.extraBookings} />
					{:else}
						<div class="text-muted-foreground">No extras booked.</div>
					{/if}
				</Card.Content>
			</Card.Root>
		</div>
	</Query>
</div>
