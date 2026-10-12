<script lang="ts">
	import { browser } from "$app/env";
	import { Alert, Button } from "@kayord/ui";
	import { createMutation, createQuery } from "@tanstack/svelte-query";
	import { paymentGetBooking, paymentInitiate, paymentVoucher, paymentVouchers } from "$lib/api/remote/payment.remote";
	import type { BookingDTO, FacilityPaymentMethodsResponse, PaymentInitiateRequest, PaymentVoucherRequest } from "$lib/api";
	import { isActiveReservation } from "$lib/booking/status";
	import { rememberPaymentReturn } from "$lib/booking/payment-return-context";
	import { paymentErrorMessage, paymentMessage, validatePaymentAmount } from "$lib/booking/payments";
	import VoucherCard from "./voucher-card.svelte";
	import BookingPaymentDetails from "./booking-payment-details.svelte";

	let {
		bookingId,
		booking,
		methods,
		onrefresh,
		onbusy,
		oneditable,
	}: {
		bookingId: number;
		booking: Pick<BookingDTO, "bookingStatusId" | "expiresAt">;
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
	let now = $state(Date.now());
	const checkoutAllowed = $derived(isActiveReservation(booking, now));
	const history = createQuery(() => ({
		queryKey: ["booking-payments", bookingId],
		retry: false,
		enabled: browser,
		queryFn: async () => {
			now = Date.now();
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
	async function pay(providerName: string, _amount: number) {
		now = Date.now();
		const amount = history.data?.amountOutstanding ?? 0;
		if (
			!checkoutAllowed ||
			unavailable ||
			refreshError ||
			!history.data ||
			history.data.isPaid ||
			history.data.amountAvailable !== history.data.amountOutstanding ||
			validatePaymentAmount(String(amount), history.data.amountAvailable)
		)
			return;
		locked = true;
		onbusy(true);
		error = "";
		message = "";
		let redirecting = false;
		try {
			const response = await initiate.mutateAsync({ bookingId, providerName, amount });
			rememberPaymentReturn(response.transactionId, bookingId);
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
		now = Date.now();
		if (!checkoutAllowed || unavailable || refreshError || vouchers.isError || !history.data || history.data.isPaid || history.data.amountAvailable <= 0)
			return;
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
		<BookingPaymentDetails
			balance={history.data}
			{methods}
			{busy}
			checkoutDisabled={unavailable || !!refreshError}
			refreshing={history.isFetching}
			{checkoutAllowed}
			onpay={pay}
			onrefresh={refresh}
		>
			{#snippet customerOperations()}
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
								disabled={history.data!.amountAvailable <= 0 || !!refreshError}
								onredeem={redeemVoucher}
							/>{/each}
					</div>{/if}
			{/snippet}
		</BookingPaymentDetails>
	{/if}
</section>
