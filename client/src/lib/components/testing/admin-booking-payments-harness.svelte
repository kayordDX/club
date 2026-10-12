<script lang="ts">
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import AdminBookingPayments from "../admin-booking-payments.svelte";
	import type { ComponentProps } from "svelte";
	let props: ComponentProps<typeof AdminBookingPayments> = $props();
	const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
</script>

<QueryClientProvider {client}><AdminBookingPayments {...props} /></QueryClientProvider>
