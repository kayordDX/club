<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";

	import { Badge, Button, Card, Table, ToggleGroup } from "@kayord/ui";
	import {
		CalendarDaysIcon,
		ChevronLeftIcon,
		Clock3Icon,
		CoinsIcon,
		CreditCardIcon,
		UserRoundIcon,
		WalletMinimalIcon,
	} from "@lucide/svelte";
	import { createBookingGet, createBookingUpdateStatus, BookingStatusEnum } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import CountdownTimer from "$lib/components/CountdownTimer.svelte";
	import { toast } from "svelte-sonner";
	import { goto } from "$app/navigation";

	const slug = $derived(page.params.slug ?? "");
	const facilityId = $derived(Number(page.params.id) || 0);

	const bookingId = $derived(Number(page.params.bookingId) || 0);
	const query = createBookingGet(() => bookingId);

	const updateStatusMut = createBookingUpdateStatus();
	const updateStatus = async (status: BookingStatusEnum) => {
		try {
			await updateStatusMut.mutateAsync({ data: { bookingId, status } });
			toast.info("Confirmed booking");
			goto(resolve(`/outlet/${slug}/book/${facilityId}`));
		} catch (error) {
			console.error("Failed to update booking status:", error);
			toast.error("Failed to update booking status. Please try again.");
		}
	};
</script>

<Query {query} emptyText="Booking not found">
	<div class="mx-auto flex w-full flex-col gap-6">
		<div class="grid gap-4">
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
					<ToggleGroup.Root type="single" class="w-full border" orientation="vertical">
						<ToggleGroup.Item value="credit" class="flex h-fit flex-1 p-4">
							<WalletMinimalIcon />
							<div>Wallet</div>
						</ToggleGroup.Item>
						<ToggleGroup.Item value="cash" class="flex h-fit flex-1 border-t-1 p-4">
							<CoinsIcon />
							<div>Pay Later</div>
						</ToggleGroup.Item>
					</ToggleGroup.Root>
				</Card.Content>
				<Card.Footer class=" flex justify-between border-t">
					<Button onclick={() => updateStatus(BookingStatusEnum.Cancelled)} variant="destructive">
						<ChevronLeftIcon class="size-4" />
						Cancel
					</Button>
					<Button onclick={() => updateStatus(BookingStatusEnum.Confirmed)}>Complete</Button>
				</Card.Footer>
			</Card.Root>
		</div>
	</div>
</Query>
