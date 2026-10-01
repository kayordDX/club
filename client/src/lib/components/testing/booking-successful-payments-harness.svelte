<script lang="ts">
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import BookingSuccessfulPayments from "../booking/booking-successful-payments.svelte";

	let { bookingId }: { bookingId: number } = $props();
	const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
</script>

<QueryClientProvider {client}><BookingSuccessfulPayments {bookingId} /></QueryClientProvider>
