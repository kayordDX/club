<script lang="ts">
	import { page } from "$app/state";
	import { onMount } from "svelte";
	import { Button } from "@kayord/ui";
	import { createQuery } from "@tanstack/svelte-query";
	import { paymentGetBooking } from "$lib/api/remote/payment.remote";
	import { bookingGet, bookingGetPath } from "$lib/api/remote/booking.remote";
	import { getBookingPayUrl } from "$lib/booking/payUrl";
	import PaymentResultCard from "$lib/components/payment-result-card.svelte";

	const transactionId = $derived(page.url.searchParams.get("transactionId") ?? "");
	let bookingId = $state(0);
	onMount(() => {
		try {
			bookingId = Number(sessionStorage.getItem(`payment-booking:${transactionId}`)) || 0;
		} catch {
			/* Storage can be unavailable in private browsing. */
		}
	});
	const result = createQuery(() => ({
		queryKey: ["payment-return", transactionId, bookingId],
		enabled: bookingId > 0,
		queryFn: async () => {
			await Promise.all([paymentGetBooking(bookingId).refresh(), bookingGet(bookingId).refresh(), bookingGetPath(bookingId).refresh()]);
			const [balance, booking, path] = await Promise.all([paymentGetBooking(bookingId), bookingGet(bookingId), bookingGetPath(bookingId)]);
			return { balance, payUrl: getBookingPayUrl(bookingId, path, booking.slotContractBookings.length) };
		},
		refetchInterval: (query) => (query.state.data?.balance.isPaid ? false : 5000),
	}));
</script>

<div class="mx-auto mt-16 flex max-w-md flex-col items-center gap-6 px-4">
	{#if bookingId && result.isPending}<p role="status">Checking payment and remaining balance...</p>
	{:else if result.isError}<p role="alert">Unable to check your balance. Your payment may still be processing.</p>
		<Button onclick={() => result.refetch()}>Retry</Button>
	{:else}<PaymentResultCard {transactionId} balance={result.data?.balance} payUrl={result.data?.payUrl} />{/if}
</div>
