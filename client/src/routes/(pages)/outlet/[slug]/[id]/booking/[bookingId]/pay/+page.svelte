<script lang="ts">
	import { page } from "$app/state";

	import { Alert, Badge, Button, Card, Table, ToggleGroup } from "@kayord/ui";
	import {
		CalendarDaysIcon,
		ChevronLeftIcon,
		Clock3Icon,
		CreditCardIcon,
		UserRoundIcon,
	} from "@lucide/svelte";
	import {
		createBookingGet,
		createBookingUpdateStatus,
		BookingStatusEnum,
		createFacilityPaymentMethods,
	} from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import CountdownTimer from "$lib/components/CountdownTimer.svelte";
	import customInstance from "$lib/api/mutator/customInstance.svelte";
	import { toast } from "svelte-sonner";
	import { goto } from "$app/navigation";

	const slug = $derived(page.params.slug ?? "");
	const facilityId = $derived(Number(page.params.id) || 0);

	const bookingId = $derived(Number(page.params.bookingId) || 0);
	const query = createBookingGet(() => bookingId);

	const paymentMethods = createFacilityPaymentMethods(() => facilityId);

	let selectedProvider = $state("");
	let isPaying = $state(false);

	const updateStatusMut = createBookingUpdateStatus();
	const cancelBooking = async () => {
		try {
			await updateStatusMut.mutateAsync({
				data: { bookingId, status: BookingStatusEnum.Cancelled },
			});
			toast.info("Booking cancelled");
			goto(`/outlet/${slug}/${facilityId}`);
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
								Your booking is pending. Please proceed to complete booking
							</Card.Description>
							{#if query.data?.expiresAt}
								<CountdownTimer expiresAt={query.data.expiresAt} />
							{/if}
						</div>
					</div>
				</Card.Header>
				<Card.Content>
					<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
						<div class="rounded-2xl border p-4">
							<div
								class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
							>
								<CalendarDaysIcon class="size-4" />
								Date
							</div>
							<p class="mt-3 text-sm font-semibold">Date</p>
						</div>
						<div class="rounded-2xl border p-4">
							<div
								class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
							>
								<Clock3Icon class="size-4" />
								Time
							</div>
							<p class="mt-3 text-sm font-semibold">Slot Start Time</p>
						</div>
						<div class="rounded-2xl border p-4">
							<div
								class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
							>
								<UserRoundIcon class="size-4" />
								Players
							</div>
							<p class="mt-3 text-sm font-semibold">
								<Badge>{query.data?.slotContractBookings.length}</Badge>
							</p>
						</div>
						<div class="rounded-2xl border p-4">
							<div
								class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
							>
								<CreditCardIcon class="size-4" />
								Outstanding
							</div>
							<p class="mt-3 text-sm font-semibold">
								R {query.data?.amountOutstanding.toFixed(2)}
							</p>
						</div>
						<div class="rounded-2xl border p-4">
							<div
								class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
							>
								<CreditCardIcon class="size-4" />
								Paid
							</div>
							<p class="mt-3 text-sm font-semibold">
								R {query.data?.amountPaid.toFixed(2)}
							</p>
						</div>
					</div>

					<div class="text-muted-foreground mt-8 mb-2">User Summary</div>
					<Card.Root class="overflow-hidden p-0">
						<Table.Root>
							<Table.Header>
								<Table.Row>
									<Table.Head>Name</Table.Head>
									<Table.Head>Cell No</Table.Head>
									<Table.Head>Email</Table.Head>
								</Table.Row>
							</Table.Header>
							<Table.Body>
								{#each query.data?.slotContractBookings as player (player.id)}
									<Table.Row>
										<Table.Cell>{player.name}</Table.Cell>
										<Table.Cell>{player.cellphone}</Table.Cell>
										<Table.Cell>{player.email}</Table.Cell>
									</Table.Row>
								{/each}
							</Table.Body>
						</Table.Root>
					</Card.Root>

					<div class="mt-4 mb-4 flex items-center justify-between gap-4">
						<div>
							<h2 class="text-lg font-semibold">Choose payment method</h2>
							<p class="text-muted-foreground text-sm">
								Pick the payment method and proceed to payment
							</p>
						</div>
					</div>

					{#if paymentMethods.data && (paymentMethods.data.length ?? 0) > 0}
						<div class="text-muted-foreground mb-4 text-sm">Available payment methods</div>

						<ToggleGroup.Root
							variant="outline"
							type="single"
							class="w-full border"
							orientation="vertical"
							bind:value={selectedProvider}
						>
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
							<Alert.Description>
								This facility do not have any available payment methods.
							</Alert.Description>
						</Alert.Root>
					{/if}
				</Card.Content>
				<Card.Footer class="flex justify-between border-t">
					<Button onclick={cancelBooking} variant="destructive" disabled={isPaying}>
						<ChevronLeftIcon class="size-4" />
						Cancel
					</Button>
					<Button onclick={initiatePayment} disabled={!selectedProvider || isPaying}>
						{isPaying ? "Processing..." : "Pay Now"}
					</Button>
				</Card.Footer>
			</Card.Root>
		</div>
	</div>
</Query>
