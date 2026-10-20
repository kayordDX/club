<script lang="ts">
	import { browser } from "$app/env";
	import type { ResolvedPathname } from "$app/types";
	import { Alert, Button } from "@kayord/ui";
	import { createMutation, createQuery } from "@tanstack/svelte-query";
	import { adminBookingGetPayments, adminBookingAcceptPayment } from "$lib/api/remote/admin.remote";
	import type { BookingDTO } from "$lib/api";
	import { isActiveReservation } from "$lib/booking/status";
	import { paymentErrorMessage, validatePaymentAmount } from "$lib/booking/payments";
	import BookingPaymentDetails from "./booking-payment-details.svelte";

	let {
		facilityId,
		bookingId,
		booking,
		returnUrl: _returnUrl,
		customerPayUrl,
		disabled = false,
		onrefresh,
		onbusy,
	}: {
		facilityId: number;
		bookingId: number;
		booking: Pick<BookingDTO, "bookingStatusId" | "expiresAt"> & Partial<Pick<BookingDTO, "userId" | "guestName">>;
		returnUrl: ResolvedPathname;
		customerPayUrl?: ResolvedPathname;
		disabled?: boolean;
		onrefresh: () => Promise<void>;
		onbusy: (_busy: boolean) => void;
	} = $props();
	let locked = $state(false);
	let error = $state("");
	let refreshError = $state("");
	let needsReview = $state(false);
	let copyMessage = $state("");
	let copyError = $state("");
	let copying = $state(false);
	let now = $state(Date.now());
	const checkoutAllowed = $derived(isActiveReservation(booking, now));
	const history = createQuery(() => ({
		queryKey: ["admin-booking-payments", facilityId, bookingId],
		retry: false,
		enabled: browser,
		queryFn: async () => {
			now = Date.now();
			const request = adminBookingGetPayments({ facilityId, id: bookingId });
			await Promise.all([request.refresh(), onrefresh()]);
			return await request;
		},
		refetchInterval: locked ? false : 10000,
	}));
	const initiate = createMutation(() => ({
		mutationFn: (body: { amount: number }) => adminBookingAcceptPayment({ facilityId, id: bookingId, body }),
	}));
	const busy = $derived(locked || initiate.isPending);
	const unavailable = $derived(disabled || busy || history.isFetching || history.isError || needsReview || !!refreshError);

	async function refresh(manual = true) {
		refreshError = "";
		try {
			await history.refetch({ throwOnError: true });
			if (manual) needsReview = false;
		} catch {
			refreshError = "Unable to refresh payment details. Refresh before making another payment; do not resubmit an uncertain payment.";
		}
	}

	async function pay(_providerName: string, amount: number) {
		now = Date.now();
		if (!checkoutAllowed || unavailable || !history.data || history.data.isPaid || validatePaymentAmount(String(amount), history.data.amountAvailable)) return;
		locked = true;
		onbusy(true);
		error = "";
		try {
			await initiate.mutateAsync({ amount });
			await refresh(false);
		} catch (cause) {
			needsReview = true;
			error = `${paymentErrorMessage(cause, "Accepting payment failed.")} Check the payment history and refresh before trying again.`;
			await refresh(false);
		} finally {
			locked = false;
			onbusy(false);
		}
	}

	async function copyCustomerUrl() {
		if (!customerPayUrl || copying) return;
		copying = true;
		copyMessage = "";
		copyError = "";
		try {
			await navigator.clipboard.writeText(new URL(customerPayUrl, window.location.origin).href);
			copyMessage = "Customer payment URL copied. The booking owner must sign in to access it.";
		} catch {
			copyError = "Unable to copy the URL. Copy the customer payment link below manually.";
		} finally {
			copying = false;
		}
	}
</script>

<section class="mt-6 space-y-5" aria-label="Booking payments">
	<h2 class="text-lg font-semibold">Payments</h2>
	<p class="text-muted-foreground text-sm">Confirmation reserves the booking; it does not record payment.</p>
	{#if booking.guestName && !booking.userId}
		<p class="text-muted-foreground text-sm">Guest booking: accept payment in the club. There is no customer account or self-service payment link.</p>
	{:else}
		<div class="space-y-2">
			<Button variant="outline" disabled={!customerPayUrl || copying} onclick={copyCustomerUrl}>{copying ? "Copying..." : "Copy customer payment URL"}</Button>
			<p class="text-muted-foreground text-sm">This is the existing authenticated customer link, not an anonymous or bearer payment link.</p>
			{#if copyMessage}<p role="status">{copyMessage}</p>{/if}
			{#if copyError}<p role="alert">{copyError}</p>
				{#if customerPayUrl}<a href={customerPayUrl}>Customer payment link (sign-in required)</a>{/if}{/if}
		</div>
	{/if}
	{#if error}<Alert.Root variant="destructive"><Alert.Title>Payment needs review</Alert.Title><Alert.Description>{error}</Alert.Description></Alert.Root>{/if}
	{#if refreshError}<p role="alert">{refreshError}</p>{/if}
	{#if history.isPending}
		<p role="status">Loading payment details...</p>
	{:else if history.isError}
		<Alert.Root variant="destructive"
			><Alert.Title>Unable to load payments</Alert.Title><Alert.Description>{paymentErrorMessage(history.error, "Refresh to try again.")}</Alert.Description
			></Alert.Root
		>
		<Button variant="outline" disabled={busy || history.isFetching} onclick={() => refresh()}>Retry</Button>
	{:else if history.data}
		<BookingPaymentDetails
			balance={history.data}
			methods={[]}
			acceptInClub
			{busy}
			checkoutDisabled={unavailable}
			refreshing={history.isFetching}
			{checkoutAllowed}
			onpay={pay}
			onrefresh={() => refresh()}
		/>
	{/if}
</section>
