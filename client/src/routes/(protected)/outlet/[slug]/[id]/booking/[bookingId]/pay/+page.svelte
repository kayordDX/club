<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";

	import { Badge, Button, Card, Table } from "@kayord/ui";
	import { CalendarDaysIcon, ChevronLeftIcon, Clock3Icon, MapPinIcon, StoreIcon, UserRoundIcon } from "@lucide/svelte";
	import { BookingStatusEnum } from "$lib/api";
	import { bookingGet, bookingGetPath, bookingUpdateStatus } from "$lib/api/remote/booking.remote";
	import { facilityPaymentMethods } from "$lib/api/remote/facility.remote";
	import BookingPayments from "$lib/components/booking-payments.svelte";
	import BookingExtras from "$lib/components/BookingExtras.svelte";
	import { formatCurrency, formatDate, formatTime } from "$lib/booking/format";
	import CountdownTimer from "$lib/components/CountdownTimer.svelte";
	import { toast } from "svelte-sonner";
	import { goto } from "$app/navigation";
	import { canReturnToBasket, getBasketUrl } from "./navigation";

	const slug = page.params.slug ?? "";
	const facilityId = Number(page.params.id) || 0;
	const bookingId = Number(page.params.bookingId) || 0;

	let booking = $state(await bookingGet(bookingId));
	async function refreshBooking() {
		await bookingGet(bookingId).refresh();
		booking = await bookingGet(bookingId);
	}
	const path = await bookingGetPath(bookingId);
	const paymentMethods = await facilityPaymentMethods(facilityId);

	const players = $derived(booking.slotContractBookings ?? []);
	const extras = $derived(booking.extraBookings ?? []);
	const playersTotal = $derived(players.reduce((sum, player) => sum + (player.slotContract?.price ?? 0), 0));
	const extrasTotal = $derived(extras.reduce((sum, extra) => sum + (extra.extra?.price ?? 0) * (extra.amount ?? 0), 0));
	const subtotal = $derived(playersTotal + extrasTotal);

	let isPaying = $state(false);

	let editable = $state(false);
	const basketUrl = $derived(
		getBasketUrl({
			slug,
			facilityId,
			searchParams: page.url.searchParams,
			booking,
		})
	);
	const canGoBack = $derived(editable && canReturnToBasket(booking) && booking.bookingStatus?.id === BookingStatusEnum.Pending);

	const cancelBooking = async () => {
		try {
			await bookingUpdateStatus({ bookingId, status: BookingStatusEnum.Cancelled });
			toast.info("Booking cancelled");
			if (basketUrl) {
				goto(basketUrl);
			} else {
				goto(resolve(`/outlet/${slug}/${facilityId}`));
			}
		} catch (error) {
			console.error("Failed to cancel booking:", error);
			toast.error("Failed to cancel booking. Please try again.");
		}
	};

	const goToEditBooking = () => {
		goto(resolve(`/bookings/${bookingId}/edit`));
	};
</script>

<div class="mx-auto flex w-full flex-col gap-6">
	<div class="grid gap-4 pt-4">
		<Card.Root class="border-border/60 overflow-hidden border shadow-sm">
			<Card.Header class="border-border/60  border-b">
				<div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
					<div class="space-y-2">
						<Card.Title class="text-2xl">Payment details</Card.Title>
						<Card.Description>
							{#if booking.bookingStatus?.id === 1}
								Your booking is pending. Please proceed to complete booking.
							{:else if booking.bookingStatus?.id === 2}
								Your booking is confirmed.
							{:else if booking.bookingStatus?.id === 3}
								Your booking has been cancelled.
							{:else if booking.bookingStatus?.id === 4}
								Your booking has expired.
							{/if}
						</Card.Description>
						{#if booking.expiresAt && booking.bookingStatus?.id === 1 && booking.amountPaid === 0}
							<CountdownTimer expiresAt={booking.expiresAt} />
						{/if}
					</div>
				</div>
			</Card.Header>
			<Card.Content>
				<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
					<div class="rounded-2xl border p-4">
						<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
							<CalendarDaysIcon class="size-4" />
							Date
						</div>
						<p class="mt-3 text-sm font-semibold">
							{formatDate(booking.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}
						</p>
					</div>
					<div class="rounded-2xl border p-4">
						<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
							<Clock3Icon class="size-4" />
							Time
						</div>
						<p class="mt-3 text-sm font-semibold">
							{formatTime(booking.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}
						</p>
					</div>
					<div class="rounded-2xl border p-4">
						<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
							<UserRoundIcon class="size-4" />
							Players
						</div>
						<p class="mt-3 text-sm font-semibold">
							<Badge>{booking.slotContractBookings.length}</Badge>
						</p>
					</div>
				</div>

				<div class="mt-4 flex flex-wrap items-center gap-x-6 gap-y-2 rounded-2xl border p-4 text-sm">
					<span class="text-muted-foreground flex items-center gap-2">
						<StoreIcon class="size-4" />
						Outlet:
						<span class="text-foreground font-semibold">{path.outletName ?? "—"}</span>
					</span>
					<span class="text-muted-foreground flex items-center gap-2">
						<MapPinIcon class="size-4" />
						Facility:
						<span class="text-foreground font-semibold">{path.facilityName ?? "—"}</span>
					</span>
				</div>

				<div class="mt-8 mb-2 flex items-center justify-between gap-4">
					<div class="text-muted-foreground">User Summary</div>
					{#if booking.user}
						<div class="text-muted-foreground text-sm">
							Booked by
							<span class="text-foreground font-semibold">
								{booking.user.firstName}
								{booking.user.lastName}
							</span>
						</div>
					{/if}
				</div>

				<Card.Root class="overflow-hidden p-0">
					<Table.Root>
						<Table.Header>
							<Table.Row>
								<Table.Head>Player</Table.Head>
								<Table.Head>Contract</Table.Head>
								<Table.Head class="text-right">Price</Table.Head>
							</Table.Row>
						</Table.Header>
						<Table.Body>
							{#each players as player (player.id)}
								<Table.Row>
									<Table.Cell class="font-medium">{player.name}</Table.Cell>
									<Table.Cell class="text-muted-foreground">
										{#if player.slotContract}
											{player.slotContract.contractName}
											{#if player.slotContract.description}
												{player.slotContract.description}{/if}
										{:else}
											—
										{/if}
									</Table.Cell>
									<Table.Cell class="text-right">{formatCurrency(player.slotContract?.price)}</Table.Cell>
								</Table.Row>
							{/each}
						</Table.Body>
					</Table.Root>

					<div class="border-t px-6 py-4">
						<div class="flex items-center justify-between gap-4 text-sm">
							<span class="text-muted-foreground">Subtotal</span>
							<span>{formatCurrency(subtotal)}</span>
						</div>
						<div class="mt-2 flex items-center justify-between gap-4 text-sm">
							<span class="text-muted-foreground">Paid</span>
							<span>{formatCurrency(booking.amountPaid)}</span>
						</div>
						<div class="mt-3 flex items-center justify-between gap-4 border-t pt-3 text-base font-semibold">
							<span>Outstanding</span>
							<span>{formatCurrency(booking.amountOutstanding)}</span>
						</div>
					</div>
				</Card.Root>

				{#if extras.length}<BookingExtras {extras} />{/if}
				<BookingPayments
					{bookingId}
					methods={paymentMethods}
					onrefresh={refreshBooking}
					onbusy={(busy) => (isPaying = busy)}
					oneditable={(value) => (editable = value)}
				/>
			</Card.Content>
			<Card.Footer class="flex justify-between border-t">
				<div class="flex gap-2">
					{#if canGoBack}
						<Button onclick={goToEditBooking} variant="outline" disabled={isPaying}>
							<ChevronLeftIcon class="size-4" />
							Back to edit
						</Button>
					{/if}
					{#if canGoBack}<Button onclick={cancelBooking} variant="destructive" disabled={isPaying}>Cancel</Button>{/if}
				</div>
			</Card.Footer>
		</Card.Root>
	</div>
</div>
