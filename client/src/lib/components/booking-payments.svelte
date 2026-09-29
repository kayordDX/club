<script lang="ts">
	import { browser } from "$app/environment";
	import { Alert, Button, Table } from "@kayord/ui";
	import { createMutation, createQuery } from "@tanstack/svelte-query";
	import { paymentGetBooking, paymentInitiate, paymentVoucher, paymentVouchers } from "$lib/api/remote/payment.remote";
	import type { FacilityPaymentMethodsResponse, PaymentInitiateRequest, PaymentVoucherRequest } from "$lib/api";
	import { formatCurrency, formatDateTime } from "$lib/booking/format";
	import { paymentMessage } from "$lib/booking/payments";
	import VoucherCard from "./voucher-card.svelte";
	import PaymentAmountForm from "./payment-amount-form.svelte";

	let {
		bookingId,
		methods,
		onrefresh,
		onbusy,
		oneditable,
	}: {
		bookingId: number;
		methods: FacilityPaymentMethodsResponse[];
		onrefresh: () => Promise<void>;
		onbusy: (_busy: boolean) => void;
		oneditable: (_editable: boolean) => void;
	} = $props();
	let message = $state("");
	let error = $state("");
	let locked = $state(false);
	const history = createQuery(() => ({
		queryKey: ["booking-payments", bookingId],
		enabled: browser,
		queryFn: async () => {
			const request = paymentGetBooking(bookingId);
			try {
				await Promise.all([request.refresh(), onrefresh()]);
				const balance = await request;
				oneditable(!balance.isPaid && balance.amountPaid === 0 && balance.amountOutstanding === balance.amountAvailable);
				return balance;
			} catch (cause) {
				oneditable(false);
				throw cause;
			}
		},
		refetchInterval: 10000,
	}));
	const vouchers = createQuery(() => ({
		queryKey: ["booking-vouchers", bookingId, history.data?.amountPaid, history.data?.amountAvailable],
		enabled: browser,
		queryFn: async () => {
			const request = paymentVouchers(bookingId);
			await request.refresh();
			return await request;
		},
	}));
	const initiate = createMutation(() => ({ mutationFn: (request: PaymentInitiateRequest) => paymentInitiate(request) }));
	const redeem = createMutation(() => ({ mutationFn: (request: PaymentVoucherRequest) => paymentVoucher(request) }));
	const busy = $derived(locked || initiate.isPending || redeem.isPending);

	async function refresh() {
		await Promise.all([history.refetch(), vouchers.refetch()]);
	}
	async function pay(providerName: string, amount: number) {
		if (busy || !history.data || history.data.isPaid || amount > history.data.amountAvailable) return;
		locked = true;
		onbusy(true);
		error = "";
		let redirecting = false;
		try {
			const response = await initiate.mutateAsync({ bookingId, providerName, amount });
			// Provider callbacks return only the transaction ID; retain its booking for the return page.
			try {
				sessionStorage.setItem(`payment-booking:${response.transactionId}`, String(bookingId));
			} catch {
				/* Do not interrupt provider redirects when storage is unavailable. */
			}
			await refresh();
			if (!response.redirectUrl) throw new Error("No provider redirect was received.");
			window.location.href = response.redirectUrl;
			redirecting = true;
		} catch (cause) {
			error = cause instanceof Error ? cause.message : "Payment initiation failed. Please try again.";
			await refresh();
		} finally {
			if (!redirecting) {
				locked = false;
				onbusy(false);
			}
		}
	}
	async function redeemVoucher(request: PaymentVoucherRequest) {
		if (busy || !history.data || history.data.isPaid || history.data.amountAvailable <= 0) return;
		locked = true;
		onbusy(true);
		error = "";
		message = "";
		try {
			const response = await redeem.mutateAsync(request);
			message = paymentMessage(response.isPaid, response.amountOutstanding);
		} catch (cause) {
			error = cause instanceof Error ? cause.message : "Voucher redemption failed. Please try again.";
		} finally {
			await refresh();
			locked = false;
			onbusy(false);
		}
	}
</script>

<section class="mt-6 space-y-5" aria-label="Booking payments">
	<h2 class="text-lg font-semibold">Payments</h2>
	{#if message}<p role="status">{message}</p>{/if}
	{#if error}<Alert.Root variant="destructive"><Alert.Title>Payment failed</Alert.Title><Alert.Description>{error}</Alert.Description></Alert.Root>{/if}
	{#if history.isPending}
		<p role="status">Loading payment details...</p>
	{:else if history.isError}
		<Alert.Root variant="destructive"
			><Alert.Title>Unable to load payments</Alert.Title><Alert.Description>{history.error.message}</Alert.Description></Alert.Root
		>
		<Button variant="outline" onclick={refresh}>Retry</Button>
	{:else if history.data}
		<dl class="grid gap-3 sm:grid-cols-3">
			<div>
				<dt>Paid</dt>
				<dd>{formatCurrency(history.data.amountPaid)}</dd>
			</div>
			<div>
				<dt>Outstanding</dt>
				<dd>{formatCurrency(history.data.amountOutstanding)}</dd>
			</div>
			<div>
				<dt>Available to pay</dt>
				<dd>{formatCurrency(history.data.amountAvailable)}</dd>
			</div>
		</dl>
		<p class="text-muted-foreground text-sm">
			Pending payments reserve funds until the provider confirms success or failure. Only the available balance can be paid.
		</p>
		<Button variant="outline" disabled={busy || history.isFetching} onclick={refresh}>Refresh payments</Button>
		{#if history.data.isPaid}
			<p role="status">Your booking is fully paid.</p>
		{:else}
			{#if history.data.amountPaid > 0}<p role="status">
					Remaining balance: {formatCurrency(history.data.amountOutstanding)}. Pay the rest with another payment method.
				</p>{/if}
			{#if history.data.amountAvailable > 0}
				{#key history.data.amountAvailable}
					<PaymentAmountForm amountAvailable={history.data.amountAvailable} {methods} {busy} onpay={pay} />
				{/key}
			{:else}<p>All outstanding funds are reserved by pending payments. Wait for confirmation before paying again.</p>{/if}
			<h3 class="font-semibold">Your vouchers</h3>
			{#if vouchers.isPending}<p role="status">Loading vouchers...</p>
			{:else if vouchers.isError}<p role="alert">Unable to load vouchers: {vouchers.error.message}</p>
				<Button variant="outline" onclick={() => vouchers.refetch()}>Retry vouchers</Button>
			{:else if !vouchers.data?.length}<p>No vouchers available.</p>
			{:else}<div class="grid gap-4 md:grid-cols-2">
					{#each vouchers.data as voucher (voucher.grantId)}<VoucherCard
							{voucher}
							{bookingId}
							{busy}
							disabled={history.data.amountAvailable <= 0}
							onredeem={redeemVoucher}
						/>{/each}
				</div>{/if}
		{/if}
		<h3 class="font-semibold">Payment history</h3>
		{#if !history.data.payments.length}<p>No payments yet.</p>
		{:else}
			<div class="overflow-x-auto">
				<Table.Root
					><Table.Header
						><Table.Row
							><Table.Head>Date</Table.Head><Table.Head>Method / provider</Table.Head><Table.Head>Status</Table.Head><Table.Head>Amount</Table.Head></Table.Row
						></Table.Header
					><Table.Body>
						{#each history.data.payments as payment (payment.id)}<Table.Row
								><Table.Cell>{formatDateTime(payment.paymentStatusDate)}</Table.Cell><Table.Cell
									>{payment.paymentType} / {payment.providerName}
									<div class="text-muted-foreground text-xs">{payment.transactionId}</div></Table.Cell
								><Table.Cell>{payment.paymentStatus}</Table.Cell><Table.Cell>{formatCurrency(payment.amount)}</Table.Cell></Table.Row
							>{/each}
					</Table.Body></Table.Root
				>
			</div>
		{/if}
	{/if}
</section>
