<script lang="ts">
	import { browser } from "$app/env";
	import { Alert, Button, Table } from "@kayord/ui";
	import { createMutation, createQuery } from "@tanstack/svelte-query";
	import { paymentGetBooking, paymentInitiate, paymentVoucher, paymentVouchers } from "$lib/api/remote/payment.remote";
	import type { FacilityPaymentMethodsResponse, PaymentInitiateRequest, PaymentVoucherRequest } from "$lib/api";
	import { formatCurrency, formatDateTime } from "$lib/booking/format";
	import { paymentErrorMessage, paymentMessage, validatePaymentAmount } from "$lib/booking/payments";
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
	let selectionVersion = $state(0);
	let refreshError = $state("");
	const history = createQuery(() => ({
		queryKey: ["booking-payments", bookingId],
		retry: false,
		enabled: browser,
		queryFn: async () => {
			const request = paymentGetBooking(bookingId);
			try {
				await Promise.all([request.refresh(), onrefresh()]);
				const balance = await request;
				oneditable(!balance.isPaid && balance.amountPaid === 0 && !balance.payments.some((payment) => payment.paymentStatus === "Pending"));
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
		retry: false,
		enabled: browser,
		queryFn: async () => {
			const request = paymentVouchers(bookingId);
			await request.refresh();
			return await request;
		},
		refetchInterval: 10000,
	}));
	const initiate = createMutation(() => ({ mutationFn: (request: PaymentInitiateRequest) => paymentInitiate(request) }));
	const redeem = createMutation(() => ({ mutationFn: (request: PaymentVoucherRequest) => paymentVoucher(request) }));
	const busy = $derived(locked || initiate.isPending || redeem.isPending);
	const unavailable = $derived(busy || history.isFetching || history.isError || vouchers.isFetching);

	async function refresh() {
		refreshError = "";
		try {
			const balance = await history.refetch({ throwOnError: true });
			if (balance.isError) throw balance.error;
			await vouchers.refetch({ throwOnError: true });
		} catch {
			refreshError = "Unable to refresh payment details. Refresh before making another payment; do not resubmit an uncertain payment.";
			oneditable(false);
		}
	}
	async function pay(providerName: string, amount: number) {
		if (unavailable || refreshError || !history.data || history.data.isPaid || validatePaymentAmount(String(amount), history.data.amountAvailable)) return;
		locked = true;
		onbusy(true);
		error = "";
		message = "";
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
			error = `${paymentErrorMessage(cause, "Payment initiation failed.")} Check the refreshed history before trying again.`;
			await refresh();
		} finally {
			if (!redirecting) {
				locked = false;
				onbusy(false);
			}
		}
	}
	async function redeemVoucher(request: PaymentVoucherRequest) {
		if (unavailable || refreshError || vouchers.isError || !history.data || history.data.isPaid || history.data.amountAvailable <= 0) return;
		locked = true;
		onbusy(true);
		error = "";
		message = "";
		try {
			const response = await redeem.mutateAsync(request);
			message = paymentMessage(response.isPaid, response.amountOutstanding);
		} catch (cause) {
			error = `${paymentErrorMessage(cause, "Voucher redemption failed.")} Eligibility or the selected quantity may have changed. Review the refreshed vouchers and select again.`;
		} finally {
			try {
				await refresh();
			} finally {
				selectionVersion += 1;
				locked = false;
				onbusy(false);
			}
		}
	}
</script>

<section class="mt-6 space-y-5" aria-label="Booking payments">
	<h2 class="text-lg font-semibold">Payments</h2>
	{#if message}<p role="status">{message}</p>{/if}
	{#if refreshError}<p role="alert">{refreshError}</p>{/if}
	{#if error}<Alert.Root variant="destructive"><Alert.Title>Payment failed</Alert.Title><Alert.Description>{error}</Alert.Description></Alert.Root>{/if}
	{#if history.isPending}
		<p role="status">Loading payment details...</p>
	{:else if history.isError}
		<Alert.Root variant="destructive"
			><Alert.Title>Unable to load payments</Alert.Title><Alert.Description>{paymentErrorMessage(history.error, "Refresh to try again.")}</Alert.Description
			></Alert.Root
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
			Only successful payments reduce your outstanding balance. Pending or failed attempts do not count as paid; avoid completing multiple attempts for the same
			balance.
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
					<PaymentAmountForm amountAvailable={history.data.amountAvailable} {methods} busy={unavailable || !!refreshError} onpay={pay} />
				{/key}
			{:else}<p>No outstanding balance is available to pay.</p>{/if}
			<h3 class="font-semibold">Your vouchers</h3>
			{#if vouchers.isPending}<p role="status">Loading vouchers...</p>
			{:else if vouchers.isError}<p role="alert">Unable to load vouchers: {paymentErrorMessage(vouchers.error, "Refresh to try again.")}</p>
				<Button variant="outline" onclick={() => vouchers.refetch()}>Retry vouchers</Button>
			{:else if !vouchers.data?.length}<p>No vouchers available.</p>
			{:else}<div class="grid gap-4 md:grid-cols-2">
					{#each vouchers.data as voucher (`${voucher.grantId}:${selectionVersion}`)}<VoucherCard
							{voucher}
							{bookingId}
							busy={unavailable}
							disabled={history.data.amountAvailable <= 0 || !!refreshError}
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
