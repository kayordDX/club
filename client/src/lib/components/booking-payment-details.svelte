<script lang="ts">
	import type { Snippet } from "svelte";
	import { Button, Table } from "@kayord/ui";
	import type { BookingPaymentDTO, FacilityPaymentMethodsResponse } from "$lib/api";
	import { formatCurrency, formatDateTime } from "$lib/booking/format";
	import PaymentAmountForm from "./payment-amount-form.svelte";

	let {
		balance,
		methods,
		busy,
		checkoutDisabled,
		refreshing,
		checkoutAllowed,
		onpay,
		onrefresh,
		customerOperations,
		acceptInClub = false,
	}: {
		balance: BookingPaymentDTO;
		methods: FacilityPaymentMethodsResponse[];
		busy: boolean;
		checkoutDisabled: boolean;
		refreshing: boolean;
		checkoutAllowed: boolean;
		onpay: (_provider: string, _amount: number) => Promise<void>;
		onrefresh: () => Promise<void>;
		customerOperations?: Snippet;
		acceptInClub?: boolean;
	} = $props();
</script>

<dl class="grid gap-3 sm:grid-cols-3">
	<div>
		<dt>Paid</dt>
		<dd>{formatCurrency(balance.amountPaid)}</dd>
	</div>
	<div>
		<dt>Outstanding</dt>
		<dd>{formatCurrency(balance.amountOutstanding)}</dd>
	</div>
	<div>
		<dt>Available to pay</dt>
		<dd>{formatCurrency(balance.amountAvailable)}</dd>
	</div>
</dl>
<p class="text-muted-foreground text-sm">
	Only successful payments reduce the outstanding balance. Pending attempts reserve funds but do not count as paid. Review the history before starting another
	attempt.
</p>
<Button variant="outline" disabled={busy || refreshing} onclick={onrefresh}>Refresh payments</Button>
{#if balance.isPaid}
	<p role="status">This booking is fully paid.</p>
{:else if !checkoutAllowed}
	<p role="status">This booking is inactive or its hold has expired. Restore the booking before checkout.</p>
{:else}
	{#if balance.amountPaid > 0}
		<p role="status">Remaining balance: {formatCurrency(balance.amountOutstanding)}.</p>
	{/if}
	{@render customerOperations?.()}
	{#if balance.amountAvailable > 0 && (acceptInClub || balance.amountAvailable === balance.amountOutstanding)}
		{#if acceptInClub || methods.length}
			{#key balance.amountAvailable}
				<PaymentAmountForm
					amountAvailable={balance.amountAvailable}
					{methods}
					{acceptInClub}
					allowPartialAmount={acceptInClub}
					busy={busy || refreshing || checkoutDisabled}
					{onpay}
				/>
			{/key}
		{:else}
			<p>No provider payment methods are available.</p>
		{/if}
	{:else if !acceptInClub && balance.amountAvailable !== balance.amountOutstanding}
		<p role="status">A payment is pending. Resolve it before paying the full remaining balance.</p>
	{:else}
		<p>No outstanding balance is available to pay.</p>
	{/if}
{/if}
<h3 class="font-semibold">Payment history</h3>
{#if !balance.payments.length}
	<p>No payments yet.</p>
{:else}
	<div class="overflow-x-auto">
		<Table.Root>
			<Table.Header>
				<Table.Row
					><Table.Head>Date</Table.Head><Table.Head>Method / provider</Table.Head><Table.Head>Status</Table.Head><Table.Head>Amount</Table.Head></Table.Row
				>
			</Table.Header>
			<Table.Body>
				{#each balance.payments as payment (payment.id)}
					<Table.Row>
						<Table.Cell>{formatDateTime(payment.paymentStatusDate)}</Table.Cell>
						<Table.Cell
							>{payment.paymentType} / {payment.providerName}
							<div class="text-muted-foreground text-xs">{payment.transactionId}</div></Table.Cell
						>
						<Table.Cell>{payment.paymentStatus}</Table.Cell><Table.Cell>{formatCurrency(payment.amount)}</Table.Cell>
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>
	</div>
{/if}
