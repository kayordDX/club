<script lang="ts">
	import { browser } from "$app/env";
	import { createQuery } from "@tanstack/svelte-query";
	import { Button, Card, Table } from "@kayord/ui";
	import { paymentGetBooking } from "$lib/api/remote/payment.remote";
	import { formatCurrency, formatDateTime } from "$lib/booking/format";

	let { bookingId }: { bookingId: number } = $props();
	const history = createQuery(() => ({
		queryKey: ["booking-payments", bookingId],
		enabled: browser && bookingId > 0,
		retry: false,
		queryFn: async () => {
			const request = paymentGetBooking(bookingId);
			await request.refresh();
			return await request;
		},
	}));
	// Match BookingPayments.IsSettled: Completed (2) and Partial (5) both credit the booking.
	const payments = $derived(history.data?.payments.filter((payment) => payment.paymentStatusId === 2 || payment.paymentStatusId === 5) ?? []);
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>Successful payments</Card.Title>
		<Card.Description>These payments contribute to the amount paid, including partial payments and vouchers.</Card.Description>
	</Card.Header>
	<Card.Content>
		{#if history.isPending}
			<p role="status">Loading successful payments...</p>
		{:else if history.isError}
			<p role="alert">Unable to load successful payments.</p>
			<Button variant="outline" onclick={() => history.refetch()}>Retry</Button>
		{:else if !payments.length}
			<p class="text-muted-foreground text-sm">No successful payments yet.</p>
		{:else}
			<div class="overflow-x-auto">
				<Table.Root>
					<Table.Header>
						<Table.Row
							><Table.Head>Date</Table.Head><Table.Head>Method / provider</Table.Head><Table.Head>Reference</Table.Head><Table.Head class="text-right"
								>Amount</Table.Head
							></Table.Row
						>
					</Table.Header>
					<Table.Body>
						{#each payments as payment (payment.id)}
							<Table.Row>
								<Table.Cell>{formatDateTime(payment.paymentStatusDate)}</Table.Cell>
								<Table.Cell>{payment.paymentType} / {payment.providerName}</Table.Cell>
								<Table.Cell>{payment.transactionId}</Table.Cell>
								<Table.Cell class="text-right">{formatCurrency(payment.amount)}</Table.Cell>
							</Table.Row>
						{/each}
					</Table.Body>
					<Table.Footer>
						<Table.Row
							><Table.Cell colspan={3}>Amount paid</Table.Cell><Table.Cell class="text-right">{formatCurrency(history.data!.amountPaid)}</Table.Cell></Table.Row
						>
					</Table.Footer>
				</Table.Root>
			</div>
		{/if}
	</Card.Content>
</Card.Root>
