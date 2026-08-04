<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";

	import { Alert, Badge, Button, Card, ToggleGroup } from "@kayord/ui";
	import { CalendarDaysIcon, ChevronLeftIcon, Clock3Icon, CreditCardIcon, MapPinIcon, StoreIcon, UserRoundIcon } from "@lucide/svelte";
	import { BookingStatusEnum, createBookingGet, createBookingGetPath, createBookingUpdateStatus, createFacilityPaymentMethods } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import BookingPlayers from "$lib/components/BookingPlayers.svelte";
	import BookingExtras from "$lib/components/BookingExtras.svelte";
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
					<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
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
						<div class="rounded-2xl border p-4">
							<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
								<CreditCardIcon class="size-4" />
								Outstanding
							</div>
							<p class="mt-3 text-sm font-semibold">
								{formatCurrency(query.data?.amountOutstanding)}
							</p>
						</div>
						<div class="rounded-2xl border p-4">
							<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
								<CreditCardIcon class="size-4" />
								Paid
							</div>
							<p class="mt-3 text-sm font-semibold">
								{formatCurrency(query.data?.amountPaid)}
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
					<BookingPlayers players={query.data?.slotContractBookings ?? []} />

					{#if (query.data?.extraBookings.length ?? 0) > 0}
						<div class="text-muted-foreground mt-6 mb-2">Extras</div>
						<BookingExtras extras={query.data?.extraBookings ?? []} />
					{/if}

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
