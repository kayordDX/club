<script lang="ts">
	import { page } from "$app/state";
	import PaymentResultCard from "#lib/components/payment-result-card.svelte";

	const transactionId = $derived(page.url.searchParams.get("transactionId") ?? "");
	const requestedBookingId = $derived(page.url.searchParams.has("bookingId") ? Number(page.url.searchParams.get("bookingId")) : undefined);
</script>

<div class="mx-auto mt-16 flex max-w-md flex-col items-center gap-6 px-4">
	<PaymentResultCard {transactionId} {requestedBookingId} outcome="failure" error={page.url.searchParams.get("error") ?? ""} lookup />
</div>
