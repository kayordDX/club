<script lang="ts">
	import { BookIcon } from "@lucide/svelte";
	import PageHeading from "../../settings/PageHeading.svelte";
	import Query from "$lib/components/Query.svelte";
	import { createBookingGet } from "$lib/api";
	import { page } from "$app/state";
	import { Card, Table } from "@kayord/ui";

	const bookingId = $derived(Number(page.params.id) || 0);
	const query = createBookingGet(() => bookingId);

	const formatDate = (value?: string | null) => {
		if (!value) return "—";

		return new Date(value).toLocaleString();
	};

	const formatCurrency = (value?: number | null) => {
		if (value == null) return "—";

		return new Intl.NumberFormat("en-ZA", {
			style: "currency",
			currency: "ZAR",
			minimumFractionDigits: 2,
		}).format(value);
	};
</script>

<div class="m-4">
	<Query {query} emptyText="Booking not found">
		<PageHeading title="Booking" description="Booking details" icon={BookIcon} />

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
						<div class="text-muted-foreground text-sm">Status</div>
						<div>{query.data?.bookingStatus?.name ?? "—"}</div>
					</div>

					<div>
						<div class="text-muted-foreground text-sm">Status updated</div>
						<div>{formatDate(query.data?.bookingStatusDate)}</div>
					</div>

					<div>
						<div class="text-muted-foreground text-sm">Expires at</div>
						<div>{formatDate(query.data?.expiresAt)}</div>
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
					<Card.Title>User</Card.Title>
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
						<div class="space-y-3">
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
						</div>
					{:else}
						<div class="text-muted-foreground">No slot contract bookings.</div>
					{/if}
				</Card.Content>
			</Card.Root>
		</div>
	</Query>
</div>
