<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";

	import { Alert, Badge, Button, Card, Table, ToggleGroup } from "@kayord/ui";
	import { CalendarDaysIcon, ChevronLeftIcon, Clock3Icon, CreditCardIcon, MapPinIcon, StoreIcon, UserRoundIcon } from "@lucide/svelte";
	import { BookingStatusEnum, createBookingGet, createBookingGetPath, createBookingUpdateStatus, createFacilityPaymentMethods } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import { formatCurrency, formatDate, formatTime } from "$lib/booking/format";
	import CountdownTimer from "$lib/components/CountdownTimer.svelte";
	import customInstance from "$lib/api/mutator/customInstance.svelte";
	import { toast } from "svelte-sonner";
	import { goto } from "$app/navigation";
	import { canReturnToBasket, getBasketUrl } from "./navigation";

	const slug = $derived(page.params.slug ?? "");
	const facilityId = $derived(Number(page.params.id) || 0);

	const bookingId = $derived(Number(page.params.bookingId) || 0);
	const query = createBookingGet(() => bookingId);

	const pathQuery = createBookingGetPath(
		() => bookingId,
		() => ({ query: { enabled: bookingId > 0 } })
	);
	const path = $derived(pathQuery.data);

	const paymentMethods = createFacilityPaymentMethods(() => facilityId);

	const players = $derived(query.data?.slotContractBookings ?? []);
	const extras = $derived(query.data?.extraBookings ?? []);
	const playersTotal = $derived(players.reduce((sum, player) => sum + (player.slotContract?.price ?? 0), 0));
	const extrasTotal = $derived(extras.reduce((sum, extra) => sum + (extra.extra?.price ?? 0) * (extra.amount ?? 0), 0));
	const subtotal = $derived(playersTotal + extrasTotal);

	let selectedProvider = $state("");
	let isPaying = $state(false);

	const basketUrl = $derived(
		getBasketUrl({
			slug,
			facilityId,
			searchParams: page.url.searchParams,
			booking: query.data,
		})
	);
	const canGoBack = $derived(canReturnToBasket(query.data) && query.data?.bookingStatus?.id === BookingStatusEnum.Pending);

	const updateStatusMut = createBookingUpdateStatus();

	const cancelBooking = async () => {
		try {
			await updateStatusMut.mutateAsync({
				data: { bookingId, status: BookingStatusEnum.Cancelled },
			});
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

	const initiatePayment = async () => {
		if (!selectedProvider) {
			toast.error("Please select a payment method.");
			return;
		}

		isPaying = true;
		try {
			const response = await customInstance<{
				transactionId: string;
				redirectUrl: string;
				providerReference?: string | null;
			}>("/payment/initiate", {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify({
					bookingId,
					providerName: selectedProvider,
				}),
			});

			if (response.redirectUrl) {
				window.location.href = response.redirectUrl;
			} else {
				toast.error("Payment initiation failed. No redirect URL received.");
			}
		} catch (error) {
			console.error("Payment initiation failed:", error);
			toast.error("Failed to initiate payment. Please try again.");
		} finally {
			isPaying = false;
		}
	};

	const goToEditBooking = () => {
		goto(resolve(`/bookings/${bookingId}/edit`));
	};
</script>

<Query {query} emptyText="Booking not found">
	<div class="mx-auto flex w-full flex-col gap-6">
		<div class="grid gap-4 pt-4">
			<Card.Root class="border-border/60 overflow-hidden border shadow-sm">
				<Card.Header class="border-border/60  border-b">
					<div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
						<div class="space-y-2">
							<Card.Title class="text-2xl">Payment details</Card.Title>
							<Card.Description>
								{#if query.data?.bookingStatus?.id === 1}
									Your booking is pending. Please proceed to complete booking.
								{:else if query.data?.bookingStatus?.id === 2}
									Your booking is confirmed.
								{:else if query.data?.bookingStatus?.id === 3}
									Your booking has been cancelled.
								{:else if query.data?.bookingStatus?.id === 4}
									Your booking has expired.
								{/if}
							</Card.Description>
							{#if query.data?.expiresAt && query.data?.bookingStatus?.id === 1}
								<CountdownTimer expiresAt={query.data.expiresAt} />
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
								{formatDate(query.data?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}
							</p>
						</div>
						<div class="rounded-2xl border p-4">
							<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
								<Clock3Icon class="size-4" />
								Time
							</div>
							<p class="mt-3 text-sm font-semibold">
								{formatTime(query.data?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime)}
							</p>
						</div>
						<div class="rounded-2xl border p-4">
							<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
								<UserRoundIcon class="size-4" />
								Players
							</div>
							<p class="mt-3 text-sm font-semibold">
								<Badge>{query.data?.slotContractBookings.length}</Badge>
							</p>
						</div>
					</div>

					<div class="mt-4 flex flex-wrap items-center gap-x-6 gap-y-2 rounded-2xl border p-4 text-sm">
						<span class="text-muted-foreground flex items-center gap-2">
							<StoreIcon class="size-4" />
							Outlet:
							<span class="text-foreground font-semibold">{path?.outletName ?? "—"}</span>
						</span>
						<span class="text-muted-foreground flex items-center gap-2">
							<MapPinIcon class="size-4" />
							Facility:
							<span class="text-foreground font-semibold">{path?.facilityName ?? "—"}</span>
						</span>
					</div>

					<div class="mt-8 mb-2 flex items-center justify-between gap-4">
						<div class="text-muted-foreground">User Summary</div>
						{#if query.data?.user}
							<div class="text-muted-foreground text-sm">
								Booked by
								<span class="text-foreground font-semibold">
									{query.data.user.firstName}
									{query.data.user.lastName}
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
								{#each extras as extraBooking (extraBooking.extraId)}
									<Table.Row>
										<Table.Cell class="font-medium">{extraBooking.extra.name}</Table.Cell>
										<Table.Cell class="text-muted-foreground">×{extraBooking.amount}</Table.Cell>
										<Table.Cell class="text-right">{formatCurrency(extraBooking.extra.price * extraBooking.amount)}</Table.Cell>
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
								<span>{formatCurrency(query.data?.amountPaid)}</span>
							</div>
							<div class="mt-3 flex items-center justify-between gap-4 border-t pt-3 text-base font-semibold">
								<span>Outstanding</span>
								<span>{formatCurrency(query.data?.amountOutstanding)}</span>
							</div>
						</div>
					</Card.Root>

					<div class="mt-4 mb-4 flex items-center justify-between gap-4">
						<div>
							<h2 class="text-lg font-semibold">Choose payment method</h2>
							<p class="text-muted-foreground text-sm">Pick the payment method and proceed to payment</p>
						</div>
					</div>

					{#if paymentMethods.data && (paymentMethods.data.length ?? 0) > 0}
						<div class="text-muted-foreground mb-4 text-sm">Available payment methods</div>

						<ToggleGroup.Root variant="outline" type="single" class="w-full border" orientation="vertical" bind:value={selectedProvider}>
							{#each paymentMethods.data as paymentMethod (paymentMethod.providerName)}
								<ToggleGroup.Item value={paymentMethod.providerName} class="flex h-fit flex-1 p-4">
									<CreditCardIcon />
									<div>{paymentMethod.type}</div>
								</ToggleGroup.Item>
							{/each}
						</ToggleGroup.Root>
					{:else}
						<Alert.Root variant="destructive">
							<Alert.Title>No payment methods</Alert.Title>
							<Alert.Description>This facility do not have any available payment methods.</Alert.Description>
						</Alert.Root>
					{/if}
				</Card.Content>
				<Card.Footer class="flex justify-between border-t">
					<div class="flex gap-2">
						{#if canGoBack}
							<Button onclick={goToEditBooking} variant="outline" disabled={isPaying}>
								<ChevronLeftIcon class="size-4" />
								Back to edit
							</Button>
						{/if}
						<Button onclick={cancelBooking} variant="destructive" disabled={isPaying}>Cancel</Button>
					</div>
					<Button onclick={initiatePayment} disabled={!selectedProvider || isPaying}>
						{isPaying ? "Processing..." : "Pay Now"}
					</Button>
				</Card.Footer>
			</Card.Root>
		</div>
	</div>
</Query>
