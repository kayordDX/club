<script lang="ts">
	import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
	import BookingPayments from "../booking-payments.svelte";
	import type { ComponentProps } from "svelte";
	let props: ComponentProps<typeof BookingPayments> = $props();
	const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
</script>

<QueryClientProvider {client}><BookingPayments {...props} /></QueryClientProvider>
