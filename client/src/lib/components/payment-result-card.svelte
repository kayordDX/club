<script lang="ts">
	import { resolve } from "$app/paths";
	import type { ResolvedPathname } from "$app/types";
	import { onMount } from "svelte";
	import { Button, Card } from "@kayord/ui";
	import { CircleSlashIcon, CircleXIcon } from "@lucide/svelte";
	import { createQuery } from "@tanstack/svelte-query";
	import type { BookingPaymentDTO } from "$lib/api";
	import { adminBookingGetPayments } from "$lib/api/remote/admin.remote";
	import { paymentGetBooking } from "$lib/api/remote/payment.remote";
	import { bookingGet, bookingGetPath } from "$lib/api/remote/booking.remote";
	import { getBookingPayUrl } from "$lib/booking/payUrl";
	import { paymentMessage } from "$lib/booking/payments";
	import { getPaymentReturn, matchesPaymentReturn, type PaymentReturnContext } from "$lib/booking/payment-return-context";

	let {
		balance,
		payUrl,
		transactionId = "",
		outcome = "success",
		error = "",
		lookup = false,
		requestedBookingId,
	}: {
		balance?: BookingPaymentDTO;
		payUrl?: ResolvedPathname;
		transactionId?: string;
		outcome?: "success" | "failure" | "cancelled";
		error?: string;
		lookup?: boolean;
		requestedBookingId?: number;
	} = $props();

	let mounted = $state(false);
	onMount(() => {
		mounted = true;
	});
	// Reading storage here also handles client-side navigation between transaction results.
	const context = $derived(mounted && lookup ? getPaymentReturn(transactionId, requestedBookingId) : undefined);
	const result = createQuery(() => ({
		queryKey: ["payment-return", context],
		enabled: !!context,
		queryFn: async () => {
			const current = context as PaymentReturnContext;
			const history = current.admin
				? adminBookingGetPayments({ facilityId: current.admin.facilityId, id: current.bookingId })
				: paymentGetBooking(current.bookingId);
			await history.refresh();
			const verifiedBalance = await history;
			if (!matchesPaymentReturn(current, verifiedBalance)) throw new Error("Payment does not match this booking.");
			if (current.admin) return { balance: verifiedBalance, payUrl: current.admin.returnUrl, returnUrl: current.admin.returnUrl };
			await Promise.all([bookingGet(current.bookingId).refresh(), bookingGetPath(current.bookingId).refresh()]);
			const [booking, path] = await Promise.all([bookingGet(current.bookingId), bookingGetPath(current.bookingId)]);
			return { balance: verifiedBalance, payUrl: getBookingPayUrl(current.bookingId, path, booking.slotContractBookings.length), returnUrl: undefined };
		},
		refetchInterval: (query) =>
			query.state.data?.balance.payments.some((payment) => payment.transactionId === transactionId && payment.paymentStatusId === 1) ? 5000 : false,
		retry: false,
	}));
	const verifiedResult = $derived(lookup && !result.isError ? result.data : undefined);
	const currentBalance = $derived(lookup ? verifiedResult?.balance : balance);
	const currentPayUrl = $derived(lookup ? verifiedResult?.payUrl : payUrl);
	const adminReturn = $derived(verifiedResult?.returnUrl);
	const checking = $derived(!!context && result.isPending);
	const failed = $derived(!!context && result.isError);
</script>

<Card.Root class="w-full text-center">
	<Card.Content class="flex flex-col items-center gap-4 py-12">
		{#if outcome === "failure"}<CircleXIcon class="size-16 text-red-500" />
		{:else if outcome === "cancelled"}<CircleSlashIcon class="size-16 text-amber-500" />{/if}
		<Card.Title class="text-2xl"
			>{outcome === "failure"
				? "Payment Failed"
				: outcome === "cancelled"
					? "Payment Cancelled"
					: currentBalance?.isPaid
						? "Booking fully paid"
						: "Payment received"}</Card.Title
		>
		<Card.Description
			>{outcome === "success"
				? currentBalance
					? paymentMessage(currentBalance.isPaid, currentBalance.amountOutstanding)
					: "Check your booking for the latest payment status and remaining balance."
				: outcome === "failure"
					? "Something went wrong while processing your payment. Check the latest payment history before trying again."
					: "Your payment was cancelled. Check the latest booking status and available balance before trying again."}</Card.Description
		>
		{#if error}<p class="text-muted-foreground max-w-xs text-xs">{error}</p>{/if}
		{#if transactionId}<p class="text-muted-foreground text-xs">Transaction ID: {transactionId}</p>{/if}
		{#if checking}<p role="status">Checking payment and remaining balance...</p>
		{:else if failed}
			<p role="alert">Unable to verify this payment and booking. The payment may still be processing. Refresh before starting another payment.</p>
			<Button onclick={() => result.refetch()}>Retry</Button>
		{:else if outcome === "success" && currentBalance && !currentBalance.isPaid && currentPayUrl}
			<Button href={currentPayUrl}>{adminReturn ? "Manage remaining balance" : "Pay remaining balance / choose another method"}</Button>
		{/if}
		<Button href={adminReturn ?? resolve("/bookings")} variant="outline"
			>{adminReturn ? "Back to booking management" : outcome === "success" ? "View My Bookings" : "Back to Bookings"}</Button
		>
	</Card.Content>
</Card.Root>
